namespace FReactive

open System
open System.Threading

type ImmediateScheduler() =
   interface IScheduler with
      member __.Schedule f = Action(fun () -> f.Invoke())

type ThreadPoolScheduler() =
   interface IScheduler with
      member __.Schedule f =
         let callback = WaitCallback(fun _ -> f.Invoke())
         Action(fun () -> ThreadPool.QueueUserWorkItem(callback) |> ignore<bool>)

type Scheduler() =
   static let immediate = ImmediateScheduler()
   static let threadPool = ThreadPoolScheduler()
   static member Immediate = immediate
   static member ThreadPool = threadPool