module AJ05044.TypeProvider
open FSharp.Data
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open System.Reflection

[<Literal>]
let apiUrl = "https://bchi.bigcitieshealth.org/resource/ffnx-yiyc.json"
type HealthRecord = JsonProvider<apiUrl>
let allData = HealthRecord.Load(apiUrl)

[<TypeProvider>]
type AJProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces ()
    let namespace_ = "BigCitiesHealth"
    let assembly = Assembly.GetExecutingAssembly()
    let categoriesNames = query {for i in allData do select i.IndicatorCategory } |> Seq.distinct
    let makeCategoryType (categoryName: string) =
        let category = ProvidedTypeDefinition(assembly, namespace_, categoryName, Some typeof<obj>)
        let categoryData = allData |> Seq.filter (fun r -> r.IndicatorCategory = categoryName)
        let indicatorsNames = query {for i in categoryData do select i.Indicator} |> Seq.distinct
        for indicatorName in indicatorsNames do
            category.AddMembersDelayed(fun () -> 
                let indicator = ProvidedTypeDefinition(indicatorName, Some typeof<obj>)           
                let indicatorData = categoryData |> Seq.filter (fun r -> r.Indicator = indicatorName)
                let citiesNames = query {for i in indicatorData do select i.Place} |> Seq.distinct
                for cityName in citiesNames do
                    indicator.AddMembersDelayed (fun () ->  
                        let city = ProvidedTypeDefinition(cityName, Some typeof<obj>)           
                        let cityData = indicatorData |> Seq.filter (fun r -> r.Place = cityName)
                        let yearsNames = query {for i in cityData do select (string i.Year)} |> Seq.distinct
                        for yearName in yearsNames do
                            city.AddMembersDelayed (fun () ->  
                                let year = ProvidedTypeDefinition(yearName, Some typeof<obj>)           
                                let yearData = cityData |> Seq.filter (fun r -> string r.Year = yearName)
                                let gendersNames = query {for i in yearData do select i.Gender} |> Seq.distinct
                                for genderName in gendersNames do
                                    year.AddMembersDelayed (fun () ->  
                                        let gender = ProvidedTypeDefinition(genderName, Some typeof<obj>)           
                                        let genderData = yearData |> Seq.filter (fun r -> r.Gender = genderName)
                                        let racesNames = query {for i in genderData do select i.RaceEthnicity} |> Seq.distinct
                                        for racesName in gendersNames do
                                            year.AddMembersDelayed (fun () ->  
                                                let measurement = 
                                                    let value = "measurement value"
                                                    let property = ProvidedProperty(
                                                                    propertyName = "Measurement", 
                                                                    propertyType = typeof<string>, 
                                                                    IsStatic=true,
                                                                    GetterCode= (fun args -> <@@ value @@>))
                                                    property
                                                [measurement])
                                        [gender])
                                [year])
                        [city])
                [indicator])
        category
    let dataTypes = [for c in categoriesNames -> makeCategoryType c]
    do this.AddNamespace(namespace_, dataTypes)

[<assembly:TypeProviderAssembly>]
do ()