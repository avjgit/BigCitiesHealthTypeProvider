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
    let categoriesNames = query {for i in healthRecords do select i.IndicatorCategory } |> Seq.distinct
    let makeCategoryType (categoryName: string) =
        let category = ProvidedTypeDefinition(assembly, namespace_, categoryName, Some typeof<obj>)
        let categoryData = query {for i in healthRecords do where (i.IndicatorCategory = categoryName)}
        let indicatorsNames = query {for i in categoryData do select i.Indicator} |> Seq.distinct
        for indicatorName in indicatorsNames do
            let indicatorData = query {for i in categoryData do where (i.Indicator = indicatorName)}
            let citiesNames = query {for i in indicatorData do select i.Place} |> Seq.distinct
            for cityName in citiesNames do
                category.AddMembersDelayed(fun () -> 
                    let indicator = ProvidedTypeDefinition(indicatorName, Some typeof<obj>)                
                    indicator.AddMembersDelayed (fun () -> 
                        let city = ProvidedTypeDefinition(cityName, Some typeof<obj>)                
                        city.AddMembersDelayed (fun () -> 
                            let measurement = 
                                let value = "measurement value"
                                let property = ProvidedProperty(
                                                propertyName = "Measurement", 
                                                propertyType = typeof<string>, 
                                                IsStatic=true,
                                                GetterCode= (fun args -> <@@ value @@>))
                                property
                            [measurement])
                        [city])
                    [indicator])
        category
    let dataTypes = [for c in categoriesNames -> makeCategoryType c]
    do this.AddNamespace(namespace_, dataTypes)

[<assembly:TypeProviderAssembly>]
do ()