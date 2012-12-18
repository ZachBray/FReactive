namespace FReactive

open System

type ObservableState =
   | Running
   | Completed
   | Error of exn

type Subject<'a> internal (schedulerType, onDistributed, onSubscribed) =
   let mutable state = Running

   let observers = ResizeArray<'a IObserver>()

   let distribute f =
      observers |> ResizeArray.iter (fun observer -> f observer)

   let distributeOnNext x =
      onDistributed x
      observers |> ResizeArray.iter (fun observer -> observer.OnNext x)
      
   let sync = MultipleActionSynchronizer<_,_>(schedulerType, distributeOnNext, fun f -> f())
   let distributeOnNext = sync.EnqueueA
   let enqueue = sync.EnqueueB
   
   new(scheduler) = Subject(scheduler, ignore, ignore)
   new() = Subject(Immediate)

   member s.OnNext x = distributeOnNext x

   member s.OnError ex = 
      enqueue (fun () -> 
         distribute (fun observer -> observer.OnError ex)
         state <- Error ex
         observers.Clear())

   member s.OnCompleted() =
      enqueue (fun () -> 
         distribute (fun observer -> observer.OnCompleted())
         state <- Completed
         observers.Clear())

   member r.Subscribe(observer:_ IObserver) =
      //TODO: Remove allocation on subscribe?
      enqueue (fun () -> 
         match state with
         | Running ->
            onSubscribed observer
            observers.Add observer
         | Error ex -> observer.OnError ex
         | Completed -> observer.OnCompleted()
      )
      { new IDisposable with
         member s.Dispose() =
            enqueue (fun () -> observers.Remove observer |> ignore<bool>)
      }

   interface ISubject<'a> with
      member s.OnNext x = s.OnNext x
      member s.OnError ex = s.OnError ex
      member s.OnCompleted() = s.OnCompleted()
      member s.Subscribe observer = s.Subscribe observer