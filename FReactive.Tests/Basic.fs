[<NUnit.Framework.TestFixture>]
module FReactive.Tests

open FsCheck
open FsCheck.Fluent
open NUnit.Framework
open System
open System.Linq
open System.Reactive

type Old = System.Reactive.Linq.Observable

let toObs(xs:_ list) =
   {  new IObservable<_> with
         member __.Subscribe observer =
            for x in xs do observer.OnNext x
            observer.OnCompleted()
            upcast new EmptyDisposable()
   }, xs.Length
 
let (|Obs|) = toObs
let (|Abs|) = abs

// TODO: What about exceptional behaviour?
[<Test>]
let take_should_be_the_same() =
   let take_is_the_same (Obs(xs, maxN)) (Abs n) =
      // sanitize n
      let n = min maxN n
      async {
         let! ys = xs |> Obs.take n |> Obs.toArray
         let zs = Old.ToEnumerable(Old.Take(xs, n)).ToArray()
         return ys = zs
      } |> Async.RunSynchronously
   Check.QuickThrowOnFailure take_is_the_same


[<Test>]
let take_while_should_be_the_same() =
   let take_while_is_the_same (Obs(xs, _)) (y:int) =
      async {
         let! ys = xs |> Obs.takeWhile (fun x -> x < y) |> Obs.toArray
         let zs = Old.ToEnumerable(Old.TakeWhile(xs, fun x -> x < y)).ToArray()
         return ys = zs
      } |> Async.RunSynchronously
   Check.QuickThrowOnFailure take_while_is_the_same

[<Test>]
let skip_should_be_the_same() =
   let skip_is_the_same (Obs(xs, _)) (Abs n) =
      async {
         let! ys = xs |> Obs.skip n |> Obs.toArray
         let zs = Old.ToEnumerable(Old.Skip(xs, n)).ToArray()
         return ys = zs
      } |> Async.RunSynchronously
   Check.QuickThrowOnFailure skip_is_the_same

[<Test>]
let skip_while_should_be_the_same() =
   let skip_while_is_the_same (Obs(xs, _)) (y:int) =
      async {
         let! ys = xs |> Obs.skipWhile (fun x -> x < y) |> Obs.toArray
         let zs = Old.ToEnumerable(Old.SkipWhile(xs, fun x -> x < y)).ToArray()
         return ys = zs
      } |> Async.RunSynchronously
   Check.QuickThrowOnFailure skip_while_is_the_same
