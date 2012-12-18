module FReactive.Obs

open System
open System.Collections
open System.Collections.Generic
open System.Threading
   
let inline foreach f (xs:_ IObservable) =
   xs.Subscribe {
      new IObserver<_> with
         member __.OnNext x = f x
         member __.OnError ex = raise ex
         member __.OnCompleted() = ()
   }

let inline iterSafe f g h (xs:_ IObservable) =
   Consumer.from (fun x -> f x) (fun ex -> g ex) (fun () -> h())
   |> Consumer.subscribeTo xs

let subscribe consumer (xs:_ IObservable) =
   xs.Subscribe consumer

let inline create f =
   { new IObservable<_> with
      member __.Subscribe consumer = f consumer
   }

let inline defer f =
   { new IObservable<_> with
      member __.Subscribe consumer = 
         f() |> subscribe consumer
   }

let inline apply xs f =
   create <| fun consumer ->
      Consumer.from 
         (fun x -> f consumer x) 
         (fun ex -> consumer.OnError ex) 
         (fun () -> consumer.OnCompleted())
      |> Consumer.subscribeTo xs

let inline map f xs =
   apply xs <| fun consumer x -> consumer.OnNext <| f x

let inline doSideEffect f xs =
   apply xs <| fun consumer x -> f x; consumer.OnNext x

let inline collect f xs =
   apply xs <| fun consumer x ->
      for y in f x do 
         consumer.OnNext y

let inline filter f xs =
   apply xs <| fun consumer x ->
      if f x then consumer.OnNext x

let inline choose f xs =
   apply xs <| fun consumer x -> 
      match f x with
      | Some y -> consumer.OnNext y
      | None -> ()

let inline scan f seed xs =
   create <| fun consumer ->
      let acc = ref seed
      consumer.OnNext seed
      let scanPublisher =
         apply xs <| fun consumer x ->
            acc := f !acc x
            consumer.OnNext !acc
      scanPublisher |> subscribe consumer

let scheduleConsumption schedulerType xs =
   create <| fun consumer ->
      let subject = Subject(schedulerType)
      upcast new CompositeDisposable(
         xs |> subscribe subject,
         subject |> subscribe consumer)

let delay t xs =
   xs |> scheduleConsumption (OnCustomScheduler(fun f ->
      fun () -> AsyncScheduler.schedule t f  
   ))

let inline subscribeOn f xs =
   create <| fun consumer ->
      let subscription = new CompositeDisposable()
      f <| fun () -> 
         subscription <-- (xs |> subscribe consumer)
      upcast subscription

let inline subscribeOnThreadPool xs =
   xs |> subscribeOn (fun f ->
      ThreadPool.QueueUserWorkItem(fun _ -> f()) |> ignore<bool>)

let inline takeWhile f xs =
   defer <| fun () ->
      let hasCompleted = ref false
      apply xs <| fun consumer x ->
         if not !hasCompleted && f x then consumer.OnNext x
         else 
            hasCompleted := true
            consumer.OnCompleted()

let inline takeUntil f xs =
   xs |> takeWhile (fun x -> not <| f x)

let take n xs =
   defer <| fun () ->
      let count = ref 0
      apply xs <| fun consumer x ->
         if !count < n then 
            consumer.OnNext x
            incr count
         else consumer.OnCompleted()

let inline skipWhile f xs =
   defer <| fun () ->
      let hasStarted = ref false
      apply xs <| fun consumer x ->
         if !hasStarted then consumer.OnNext x
         else if f x then 
            consumer.OnNext x
            hasStarted := true

let inline skipUntil f xs =
   xs |> skipWhile (fun x -> not <| f x)

let skip n xs =
   defer <| fun () ->
      let count = ref 0
      apply xs <| fun consumer x ->
         if !count >= n then consumer.OnNext x
         else incr count

let inline scheduleAggregate chunkSeed f schedule xs =
   create <| fun consumer ->
      let acc = ref chunkSeed
      let hasAcc = ref false
      let addToAggregate (sync:MutuallyRecursiveActionSynchronizer<_,_>) =
         fun x ->
            let isFirstItemInAggregate = not !hasAcc
            acc := f !acc x
            hasAcc := true
            if isFirstItemInAggregate then
               schedule sync.EnqueueB
      let takeAggregate _ =
         fun () ->
            consumer.OnNext !acc
            acc := chunkSeed
            hasAcc := false
      let sync = MutuallyRecursiveActionSynchronizer(Immediate, addToAggregate, takeAggregate)
      let addToSample = sync.EnqueueA
      apply xs (fun _ x -> addToSample x)
      |> subscribe consumer

let inline aggregatePeriodically periodLength chunkSeed f xs =
   defer <| fun () ->
      let isFirstItemEver = ref true
      xs |> scheduleAggregate
         chunkSeed
         f
         (fun f -> 
            if !isFirstItemEver then
               isFirstItemEver := false
               f()
            else AsyncScheduler.schedule periodLength f)

let sample periodLength xs =
   xs |> aggregatePeriodically
      periodLength
      (Unchecked.defaultof<_>)
      (fun _ x -> x)

let bufferUnsafe periodLength xs =  
   xs |> aggregatePeriodically
      periodLength
      (ResizeArray())
      (fun acc x -> acc.Add x; acc)
   |> map (fun acc -> acc)

let buffer periodLength xs =  
   xs 
   |> bufferUnsafe periodLength
   |> map (fun acc ->
      let buffer = acc.ToArray()
      acc.Clear()
      buffer)

let throttle periodLength xs =
   let buffered = xs |> bufferUnsafe periodLength
   apply buffered (fun consumer acc ->
      if acc.Count = 1 then 
         consumer.OnNext acc.[0]
      acc.Clear())

let fromSeq xs =
   create <| fun consumer ->
      for x in xs do
         consumer.OnNext x
      upcast new EmptyDisposable()

let yieldReturn x =
   create <| fun consumer ->
      consumer.OnNext x
      consumer.OnCompleted()
      upcast new EmptyDisposable() 

let merge xss =
   create <| fun consumer ->
      let subscriptions = new CompositeDisposable()
      let subscriptionCount = ref 1 // Starts at 1 because of observable of observables

      let onNext x =
         if not subscriptions.IsDisposed then
            consumer.OnNext x

      let onError ex = 
         subscriptions.Dispose()
         consumer.OnError ex

      let onCompleted() =
         let newSubscriptionCount = 
            Interlocked.Decrement subscriptionCount
         if newSubscriptionCount = 0 then
            subscriptions.Dispose()
            consumer.OnCompleted()

      apply xss (fun consumer xs -> 
         Interlocked.Increment subscriptionCount |> ignore<_>
         xs |> subscribe consumer |> subscriptions.Add)
      |> iterSafe (fun x -> onNext x) (fun ex -> onError ex) (fun () -> onCompleted())
      |> subscriptions.Add

      upcast subscriptions

let mergeSeq xss =
   xss |> fromSeq |> merge

let mergeWith ys xs =
   seq { yield xs; yield ys } |> fromSeq |> merge

let switch xss =
   create <| fun consumer ->
      let subscriptions = new CompositeDisposable()
      let innerSubscription = new MutableDisposable()
      subscriptions.Add innerSubscription
      
      let hasOuterRunning = ref true
      let hasInnerRunning = ref false
      
      let onNext x =
         if not subscriptions.IsDisposed then
            consumer.OnNext x
      
      let sync = MultipleActionSynchronizer<_,_>(Immediate, onNext, fun f -> f())
      let onNext = sync.EnqueueA
      let enqueue = sync.EnqueueB
            
      let onError ex =
         if not subscriptions.IsDisposed then
            consumer.OnError ex
            subscriptions.Dispose()

      let completeIfFinished() =
         let isFinished = 
            not !hasOuterRunning && 
            not !hasInnerRunning
         if isFinished then
            consumer.OnCompleted()
            subscriptions.Dispose()

      let onOuterCompleted() =
         hasOuterRunning := false
         completeIfFinished()

      let onInnerCompleted() =
         hasInnerRunning := false
         innerSubscription.Change <| new EmptyDisposable()
         completeIfFinished()

      let swapInner xs =
         if not subscriptions.IsDisposed then
            hasInnerRunning := true
            xs |> iterSafe
               (fun x -> onNext x)
               (fun ex -> enqueue (fun () -> onError ex))
               (fun () -> enqueue onInnerCompleted)
            |> innerSubscription.Change

      xss 
      |> iterSafe
         (fun xs -> enqueue (fun () -> swapInner xs))
         (fun ex -> enqueue (fun () -> onError ex))
         (fun () -> enqueue onOuterCompleted)
      |> subscriptions.Add

      upcast subscriptions

let inline fromEvent createHandler  =
   create <| fun consumer ->
      new DisposableAction(createHandler (fun x -> consumer.OnNext x)) 
      :> IDisposable

let inline multicast createSubject (xs:_ IObservable) =
   let subject: _ ISubject = createSubject()
   { new IPublishedObservable<_> with
      member __.Subscribe consumer = subject.Subscribe consumer
      member __.Connect() = xs.Subscribe subject
   }

let publish xs =
   xs |> multicast (fun () -> upcast Subject())

let replayOne xs =
   xs |> multicast (fun () -> upcast ReplayOneSubject())

let interval t =
   defer <| fun () ->
      let output = Subject()
      let count = ref 0L
      let scheduleAndEmit = ref Unchecked.defaultof<_>
      scheduleAndEmit :=
         fun () ->
            output.OnNext !count
            count := !count + 1L
            AsyncScheduler.schedule t !scheduleAndEmit
      output

let refCount (xs:_ IPublishedObservable) =
   let referenceCount = ref 0
   let subscription = ref Unchecked.defaultof<_>
   let addConsumer() =
      incr referenceCount
      if !referenceCount = 1 then
         subscription := xs.Connect()
   let removeConsumer() =
      decr referenceCount
      if !referenceCount = 0 then
         (!subscription).Dispose()
         subscription := Unchecked.defaultof<_>
   let sync = MultipleActionSynchronizer<_,_>(Immediate, addConsumer, removeConsumer)
   let addConsumer = sync.EnqueueA
   let removeConsumer = sync.EnqueueB
   create <| fun consumer ->
      let subscription = xs |> subscribe consumer
      addConsumer()
      let dispose() =
         subscription.Dispose()
         removeConsumer()
      upcast new DisposableAction(dispose)

let inline never<'a> : 'a IObservable =
   create <| fun _ ->
      upcast new EmptyDisposable()

let inline distinctBy f xs =
   defer <| fun () ->
      let seen = HashSet()
      apply xs (fun consumer x ->
         if seen.Add <| f x then 
            consumer.OnNext x)

let inline distinctUntilChanged f xs =
   defer <| fun () ->
      let lastSeen = ref Unchecked.defaultof<_>
      let hasLastSeen = ref false
      let comparer = EqualityComparer.Default
      apply xs (fun consumer x ->
         let y = f x
         if not !hasLastSeen || not <| comparer.Equals(!lastSeen, y) then
            consumer.OnNext x
         hasLastSeen := true
         lastSeen := y)