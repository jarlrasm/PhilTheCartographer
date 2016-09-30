namespace Phil.Core

type NamedSymbol(name:string , typesymbol:Microsoft.CodeAnalysis.ITypeSymbol) =
    member this.Name=name
    member this.TypeSymbol=typesymbol

