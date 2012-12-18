module FReactive.Combine

open System
open System.Collections.Generic
open FReactive
open FReactive.Iterators
open FReactive.Obs

let inline private withAux f (qs:IObservableIterator seq) =
   create <| fun consumer ->
      let allReceived = List qs
      let hasCompleted = ref false

      let onNext i =
         if not !hasCompleted then
            allReceived.[i].MoveNext()
            let isWaiting = 
               allReceived |> ResizeArray.exists (fun q -> not q.HasCurrent)
            if not isWaiting then consumer.OnNext <| f()

      let sync = MultipleActionSynchronizer<_,_>(Immediate, onNext, fun f -> f())
      let onNext = sync.EnqueueA
      let enqueue = sync.EnqueueB

      let resources = new CompositeDisposable()

      let onError ex =
         if not !hasCompleted then
            consumer.OnError ex
            resources.Dispose()
            hasCompleted := true

      let onCompleted() =
         if not !hasCompleted then
            consumer.OnCompleted()
            hasCompleted := true

      allReceived |> Seq.iteri (fun i q ->
         q |> iterSafe 
            (fun () -> onNext i) 
            (fun ex -> enqueue (fun () -> onError ex))
            (fun () -> enqueue onCompleted)
         |> resources.Add
      )
      resources :> IDisposable

let inline with1 f ys xs =
   defer <| fun () ->
      let xsIt = ObservableIterator(xs)
      let ysIt = ObservableIterator(ys)
      let qs = seq { 
         yield xsIt :> IObservableIterator
         yield upcast ysIt
      }
      let combineNext =
         fun () -> f xsIt.Current ysIt.Current
      qs |> withAux combineNext

let inline with2 f ys zs xs =
   defer <| fun () ->
      let xsIt = ObservableIterator(xs)
      let ysIt = ObservableIterator(ys)
      let zsIt = ObservableIterator(zs)
      let qs = seq { 
         yield xsIt :> IObservableIterator
         yield upcast ysIt
         yield upcast zsIt
      }
      let combineNext =
         fun () -> f xsIt.Current ysIt.Current zsIt.Current
      qs |> withAux combineNext

let inline with3 f ys zs bs xs =
   defer <| fun () ->
      let xsIt = ObservableIterator(xs)
      let ysIt = ObservableIterator(ys)
      let zsIt = ObservableIterator(zs)
      let bsIt = ObservableIterator(bs)
      let qs = seq { 
         yield xsIt :> IObservableIterator
         yield upcast ysIt
         yield upcast zsIt
         yield upcast bsIt
      }
      let combineNext =
         fun () -> f xsIt.Current ysIt.Current zsIt.Current bsIt.Current
      qs |> withAux combineNext

let inline with4 f ys zs bs cs xs =
   defer <| fun () ->
      let xsIt = ObservableIterator(xs)
      let ysIt = ObservableIterator(ys)
      let zsIt = ObservableIterator(zs)
      let bsIt = ObservableIterator(bs)
      let csIt = ObservableIterator(cs)
      let qs = seq { 
         yield xsIt :> IObservableIterator
         yield upcast ysIt
         yield upcast zsIt
         yield upcast bsIt
         yield upcast csIt
      }
      let combineNext =
         fun () -> f xsIt.Current ysIt.Current zsIt.Current bsIt.Current csIt.Current
      qs |> withAux combineNext

let inline with5 f ys zs bs cs ds xs =
   defer <| fun () ->
      let xsIt = ObservableIterator(xs)
      let ysIt = ObservableIterator(ys)
      let zsIt = ObservableIterator(zs)
      let bsIt = ObservableIterator(bs)
      let csIt = ObservableIterator(cs)
      let dsIt = ObservableIterator(ds)
      let qs = seq { 
         yield xsIt :> IObservableIterator
         yield upcast ysIt
         yield upcast zsIt
         yield upcast bsIt
         yield upcast csIt
         yield upcast dsIt
      }
      let combineNext =
         fun () -> f xsIt.Current ysIt.Current zsIt.Current bsIt.Current csIt.Current dsIt.Current
      qs |> withAux combineNext