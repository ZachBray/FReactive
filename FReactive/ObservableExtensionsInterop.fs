namespace FReactive

open System
open System.Runtime.CompilerServices
open FReactive

[<Extension>]
module Observable =
   
   let [<Extension>] Subscribe xs (f:Action<_>)  =
      xs |> Obs.foreach (fun x -> f.Invoke x)
   
   let [<Extension>] Create (f:Func<_ IObserver, IDisposable>) =
      Obs.create (fun consumer -> f.Invoke consumer)

   let [<Extension>] Defer (f:Func<_ IObservable>) =
      Obs.defer (fun () -> f.Invoke())

   let [<Extension>] Select xs (f:Func<_, _>)  =
      xs |> Obs.map (fun x -> f.Invoke x)

   let [<Extension>] Do xs (f:Func<_, _>)  =
      xs |> Obs.doSideEffect (fun x -> f.Invoke x)

   let [<Extension>] SelectMany xs (f:Func<_, _ seq>)  =
      xs |> Obs.collect (fun x -> f.Invoke x)

   let [<Extension>] Where xs (f:Func<_, bool>)  =
      xs |> Obs.filter (fun x -> f.Invoke x)

   let [<Extension>] Scan xs seed (f:Func<_, _, _>)  =
      xs |> Obs.scan (fun acc x -> f.Invoke(acc, x)) seed

   let [<Extension>] ProduceOn xs (s:IScheduler) =
      xs |> Obs.scheduleConsumption (OnCustomScheduler(fun g -> (s.Schedule <| Action(fun () -> g())).Invoke))

   let [<Extension>] ProduceOnThreadPool xs =
      xs |> Obs.scheduleConsumption OnThreadPool

   let [<Extension>] Delay xs t =
      xs |> Obs.delay t

   let [<Extension>] SubscribeOn xs (s:IScheduler) =
      xs |> Obs.subscribeOn (fun g -> (s.Schedule <| Action(fun () -> g())).Invoke())

   let [<Extension>] SubscribeOnThreadPool xs =
      xs |> Obs.subscribeOnThreadPool

   let [<Extension>] TakeWhile xs (f:Func<'a, bool>) =
      xs |> Obs.takeWhile (fun x -> f.Invoke x)

   let [<Extension>] TakeUntil xs (f:Func<'a, bool>) =
      xs |> Obs.takeUntil (fun x -> f.Invoke x)

   let [<Extension>] Take xs n  =
      xs |> Obs.take n

   let [<Extension>] SkipWhile xs (f:Func<'a, bool>) =
      xs |> Obs.skipWhile (fun x -> f.Invoke x)

   let [<Extension>] SkipUntil xs (f:Func<'a, bool>) =
      xs |> Obs.skipUntil (fun x -> f.Invoke x)

   let [<Extension>] Skip xs n  =
      xs |> Obs.skip n

   let [<Extension>] BufferUnsafe xs samplePeriod =
      xs |> Obs.bufferUnsafe samplePeriod
      
   let [<Extension>] Buffer xs samplePeriod =
      xs |> Obs.buffer samplePeriod

   let [<Extension>] Sample xs samplePeriod =
      xs |> Obs.sample samplePeriod

   let [<Extension>] Throttle xs samplePeriod =
      xs |> Obs.throttle samplePeriod

   let [<Extension>] ToProducer xs =
      xs |> Obs.fromSeq
      
   let [<Extension>] Return x =
      Obs.yieldReturn x
      
   let [<Extension>] Merge (xss:_ IObservable IObservable) =
      Obs.merge xss

   let [<Extension>] MergeWith (xs:'a IObservable) (ys:'a IObservable) =
      xs |> Obs.mergeWith ys

   let [<Extension>] Switch xss =
      Obs.switch xss

   let [<Extension>] Multicast xs (createSubject:Func<_>) =
      xs |> Obs.multicast (fun () -> createSubject.Invoke())

   let [<Extension>] Publish xs =
      xs |> Obs.publish

   let [<Extension>] ReplayOne xs =
      xs |> Obs.replayOne
  
   let [<Extension>] Interval t =
      Obs.interval t

   let [<Extension>] RefCount xs =
      xs |> Obs.refCount

   let [<Extension>] Never() : 'a IObservable =
      Obs.never<'a>

   let [<Extension>] DistinctBy xs (f:Func<_,_>) =
      xs |> Obs.distinctBy f.Invoke

   let [<Extension>] DistinctUntilChanged xs =
      xs |> Obs.distinctUntilChanged (fun x -> x)

   let [<Extension>] CombineWith1 xs ys (f:Func<_,_,_>) =
      xs |> Combine.with1 (fun x y -> f.Invoke(x, y))  ys

   let [<Extension>] CombineWith2 xs ys zs (f:Func<_,_,_,_>) =
      xs |> Combine.with2 (fun x y z -> f.Invoke(x, y, z)) ys zs

   let [<Extension>] CombineWith3 xs ys zs bs (f:Func<_,_,_,_,_>) =
      xs |> Combine.with3 (fun x y z b -> f.Invoke(x, y, z, b)) ys zs bs

   let [<Extension>] CombineWith4 xs ys zs bs cs (f:Func<_,_,_,_,_,_>) =
      xs |> Combine.with4 (fun x y z b c -> f.Invoke(x, y, z, b, c)) ys zs bs cs

   let [<Extension>] CombineWith5 xs ys zs bc cs ds (f:Func<_,_,_,_,_,_,_>) =
      xs |> Combine.with5 (fun x y z b c d -> f.Invoke(x, y, z, b, c, d)) ys zs bc cs ds




[<Extension>]
module Event =
   let [<Extension>] ToObservable(f:Func<Action<_>, Action>) =
      Obs.fromEvent (fun next -> f.Invoke(Action<_>(fun x -> next x)).Invoke)