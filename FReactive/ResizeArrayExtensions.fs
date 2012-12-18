module internal FReactive.ResizeArray

open System.Collections.Generic

let inline iter f (xs:_ ResizeArray) =
   for i = 0 to xs.Count - 1 do
      f xs.[i]

let inline exists f (xs:_ ResizeArray) =
   let rec existsAux i =
      if i >= xs.Count then false
      else f xs.[i] || existsAux (i+1)
   existsAux 0
