namespace FReactive.Iterators

open FReactive
open System
open System.Collections.Concurrent

type IObservableIterator =
   inherit IObservable<unit>
   abstract HasCurrent: bool
   abstract MoveNext: unit -> unit

type 'a ObservableIterator(producer) =
   let q = ConcurrentQueue()
   let mutable value = Unchecked.defaultof<'a>
   let mutable hasValue = false
   let notify = 
      producer 
      |> Obs.doSideEffect (fun x -> q.Enqueue x)
      |> Obs.map (fun _ -> ())
   member __.Current = value
   member __.HasCurrent = hasValue   
   member __.MoveNext() = 
      hasValue <- q.TryDequeue(&value)
   interface IObservableIterator with
      member q.HasCurrent = q.HasCurrent
      member q.MoveNext() = q.MoveNext()
      member q.Subscribe consumer = notify.Subscribe consumer

