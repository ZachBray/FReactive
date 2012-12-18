namespace FReactive

open System
open System.Threading

type DisposableAction(f) =
   interface IDisposable with
      member __.Dispose() = f()

type EmptyDisposable() =
   interface IDisposable with
      member __.Dispose() = ()

type MutableDisposable() =
   let mutable inner = new EmptyDisposable() :> IDisposable
   member __.Change resource = 
      inner.Dispose()
      inner <- resource
   interface IDisposable with
      member __.Dispose() =
         inner.Dispose()

type CompositeDisposable([<ParamArray>] initialResources) =
   let mutable hasDisposed = false
   let resources = ResizeArray<IDisposable>()
   
   let add resource =
      if not hasDisposed then resources.Add resource
      else resource.Dispose()

   let disposeAll() =
      if not hasDisposed then
         hasDisposed <- true
         resources |> ResizeArray.iter (fun resource ->
            resource.Dispose())

   let reset() =
      if not hasDisposed then
         disposeAll()
         hasDisposed <- false

   do initialResources |> Array.iter add

   let sync = MultipleActionSynchronizer<_,_,_>(Immediate, add, disposeAll, reset)
   let add = sync.EnqueueA
   let disposeAll = sync.EnqueueB
   let reset = sync.EnqueueC
   
   member __.IsDisposed = hasDisposed

   member __.Add resource = add resource      

   member __.DisposeAndReset() = reset()

   member __.Dispose() = disposeAll()
   
   static member (<--) (ref:CompositeDisposable, r) = ref.Add r

   interface IDisposable with
      member ref.Dispose() = ref.Dispose()