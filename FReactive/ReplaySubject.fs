namespace FReactive

open System
open System.Collections.Generic

type ReplaySubject<'a>(schedulerType, maxReplayCount) =
   let mutable oldNotifications = Queue()

   let trackNotification x =
      oldNotifications.Enqueue x
      if oldNotifications.Count > maxReplayCount then
         oldNotifications.Dequeue() |> ignore<'a>

   let replayNotifications(observer:IObserver<'a>) =
      oldNotifications.ToArray()
      |> Array.iter (fun notification -> 
         observer.OnNext notification)

   let innerSubject = Subject(schedulerType, trackNotification, replayNotifications)

   new n = ReplaySubject<'a>(Immediate, n)

   member s.OnNext x = innerSubject.OnNext x
   member s.OnError ex = innerSubject.OnError ex
   member s.OnCompleted() = innerSubject.OnCompleted()
   member r.Subscribe(observer:_ IObserver) = innerSubject.Subscribe observer

   interface ISubject<'a> with
      member s.OnNext x = s.OnNext x
      member s.OnError ex = s.OnError ex
      member s.OnCompleted() = s.OnCompleted()
      member s.Subscribe observer = s.Subscribe observer

type 'a ReplayOneSubject(scheduler) =
   inherit ReplaySubject<'a>(scheduler, 1)
   new() = ReplayOneSubject(Immediate)
