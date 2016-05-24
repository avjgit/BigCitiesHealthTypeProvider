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
        let categorie = ProvidedTypeDefinition(assembly, namespace_, categoryName, Some typeof<obj>)

        let indicators = query {for i in healthRecords do
                                where (i.IndicatorCategory = categoryName) 
                                select i.Indicator 
                                distinct}

        for i in indicators do
            categorie.AddMembersDelayed(fun () -> 
                let indicator = ProvidedTypeDefinition(i, Some typeof<obj>)
                indicator.AddMembersDelayed (fun () -> 
                    let indicatorValue = 
                        let iValue = "indicator value"
                        let p = ProvidedProperty(
                                    propertyName = "Value", 
                                    propertyType = typeof<string>, 
                                    IsStatic=true,
                                    GetterCode= (fun args -> <@@ iValue @@>))
                        p
                    [indicatorValue])
                [indicator])
        categorie

    let categories = query {for i in healthRecords do 
                            select i.IndicatorCategory 
                            distinct}
    let dataTypes = [for c in categories -> makeCategoryType c]
    do this.AddNamespace(namespace_, dataTypes)

[<assembly:TypeProviderAssembly>]
do ()