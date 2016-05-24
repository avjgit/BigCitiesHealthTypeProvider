module AJ05044.TypeProvider

open FSharp.Data
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open System.Reflection

[<Literal>]
let apiUrl = "https://bchi.bigcitieshealth.org/resource/ffnx-yiyc.json"
type HealthRecord = JsonProvider<apiUrl>
let healthRecords = HealthRecord.Load(apiUrl)

[<TypeProvider>]
type AJProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces ()
    
    let namespace_ = "BigCitiesHealth"
    let assembly = Assembly.GetExecutingAssembly()

    let makeCategoryType (categoryName: string) =
        let t = ProvidedTypeDefinition(assembly, namespace_, categoryName, Some typeof<obj>)
        let prop = ProvidedProperty(propertyName = "SomeProperty", 
                                    propertyType = typeof<string>, 
                                    IsStatic=true,
                                    GetterCode= (fun args -> <@@ "demo value" @@>))
        t.AddMember prop
        t

    let categories = query {for i in healthRecords do 
                                select i.IndicatorCategory 
                                distinct}

    let dataTypes = [for i in categories -> makeCategoryType i]
    
    do this.AddNamespace(namespace_, dataTypes)

[<assembly:TypeProviderAssembly>]
do ()