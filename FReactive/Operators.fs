[<AutoOpen>]
module internal FReactive.Operators

let inline (==) x y = obj.ReferenceEquals(x, y) 

let inline (!=) x y = not (x == y)