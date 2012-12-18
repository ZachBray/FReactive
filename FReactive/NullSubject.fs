namespace FReactive

type 'a NullSubject() =
   interface ISubject<'a> with
      member __.Subscribe _ = upcast new EmptyDisposable()
      member __.OnNext _ = ()
      member __.OnError _ = ()
      member __.OnCompleted() = ()