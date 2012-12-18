namespace FReactive

open System

type Unit = struct end

type IPublishedObservable<'a> =
   inherit IObservable<'a>
   abstract Connect: unit -> IDisposable

type ITransformer<'a, 'b> =
   inherit IObserver<'a>
   inherit IObservable<'b>

type ISubject<'a> =
   inherit ITransformer<'a, 'a>

type SchedulerType =
   | Immediate
   | OnThreadPool
   | OnCustomScheduler of ((unit -> unit) -> (unit -> unit))

/// For C# customization
type IScheduler =
   abstract Schedule: Action -> Action