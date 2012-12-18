namespace FReactive

open System
open System.Collections.Generic

module AsyncScheduler =
   let private tickSize = 50
   let private tickCount = ref 0L
   let private actions = Dictionary()
   let private errors = Subject()
   let attempt action =
      try action()
      with ex -> errors.OnNext ex

   let private scheduleActionAux(task:KeyValuePair<_,_>) =
      let dueTime = task.Key
      let action = task.Value
      if dueTime < !tickCount then
         attempt action
      else
         match actions.TryGetValue dueTime with
         | false, _ ->
            let actionsAtDueTime = ResizeArray()
            actions.Add(dueTime, actionsAtDueTime)
            actionsAtDueTime.Add action
         | true, actionsAtDueTime ->
            actionsAtDueTime.Add action

   let private processTickAux() =
      tickCount := !tickCount + 1L
      let currentTime = !tickCount
      match actions.TryGetValue currentTime with
      | false, _ -> ()
      | true, dueActions ->
         actions.Remove currentTime |> ignore<bool>
         for action in dueActions do
            attempt action

   let private sync = MultipleActionSynchronizer<_,_>(Immediate, scheduleActionAux, processTickAux)
   let private scheduleAction dueTime action = sync.EnqueueA <| KeyValuePair(dueTime, action)
   let private processTick() = sync.EnqueueB()
   let private timer = new System.Threading.Timer((fun _ -> processTick()), null, 0, tickSize)
   
   let schedule (timeSpan:TimeSpan) f =
      let milliseconds = int timeSpan.TotalMilliseconds
      let ticksAhead = milliseconds / tickSize
      let dueTime = !tickCount + int64 ticksAhead
      scheduleAction dueTime f