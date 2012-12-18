module FReactive.Consumer

open System

let subscribeTo (producer:_ IObservable) (consumer:_ IObserver) =
   producer.Subscribe consumer

let inline from f g h =
   { new IObserver<_> with
       member __.OnNext x = f x
       member __.OnError ex = g ex
       member __.OnCompleted() = h()   
   }