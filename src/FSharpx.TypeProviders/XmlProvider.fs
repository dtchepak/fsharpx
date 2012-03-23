﻿// ----------------------------------------------------------------------------
// Original Xml type provider
// (c) Tomas Petricek - tomasP.net, Available under Apache 2.0 license.
// ----------------------------------------------------------------------------
module FSharpx.TypeProviders.XmlTypeProvider

open System
open FSharpx.TypeProviders.DSL
open Microsoft.FSharp.Quotations
open Samples.FSharp.ProvidedTypes
open FSharpx.TypeProviders.Inference
open System.Xml.Linq

/// Generates type for an inferred XML element
let rec generateType (ownerType:ProvidedTypeDefinition) (CompoundProperty(elementName,multi,elementChildren,elementProperties)) =
    let ty = runtimeType<TypedXElement> elementName
    ownerType.AddMember(ty)

    let accessExpr propertyName propertyType (args: Expr list) = 
        match propertyType with
        | x when x = typeof<bool> ->
            <@@  let (s:string) = (%%args.[0]:TypedXElement).Element.Attribute(XName.op_Implicit propertyName).Value
                 s.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
                 s.Equals("yes", StringComparison.InvariantCultureIgnoreCase)  @@> 
        | x when x = typeof<int> ->
            <@@  (%%args.[0]:TypedXElement).Element.Attribute(XName.op_Implicit propertyName).Value
                    |> Int32.Parse @@> 
        | x when x = typeof<float> ->
            <@@  (%%args.[0]:TypedXElement).Element.Attribute(XName.op_Implicit propertyName).Value
                    |> Double.Parse @@> 
        | x when x = typeof<string> ->
            <@@  (%%args.[0]:TypedXElement).Element.Attribute(XName.op_Implicit propertyName).Value @@> 

    let checkIfOptional propertyName (args: Expr list) = 
        <@@ (%%args.[0]:TypedXElement).Element.Attribute(XName.op_Implicit propertyName) <> null @@>

    let setterExpr propertyName propertyType (args: Expr list) = 
        match propertyType with
        | x when x = typeof<bool> ->
            <@@  (%%args.[0]:TypedXElement).Element.
                Attribute(XName.op_Implicit propertyName).Value <- (%%args.[1]:bool).ToString() @@>
        | x when x = typeof<int> ->
            <@@  (%%args.[0]:TypedXElement).Element.
                Attribute(XName.op_Implicit propertyName).Value <- (%%args.[1]:int).ToString() @@>
        | x when x = typeof<float> ->
            <@@  (%%args.[0]:TypedXElement).Element.
                Attribute(XName.op_Implicit propertyName).Value <- (%%args.[1]:float).ToString() @@>
        | x when x = typeof<string> ->
            <@@  (%%args.[0]:TypedXElement).Element.
                Attribute(XName.op_Implicit propertyName).Value <- (%%args.[1]:string) @@>

    generateProperties ty accessExpr checkIfOptional setterExpr elementProperties

    let multiAccessExpr childName (args: Expr list) =
        <@@ seq { for e in ((%%args.[0]:TypedXElement).Element.Elements(XName.op_Implicit childName)) -> 
                                TypedXElement(e) } @@>

    let singleAccessExpr childName (args: Expr list) = raise <| new NotImplementedException()

    generateSublements ty ownerType multiAccessExpr singleAccessExpr generateType elementChildren   

/// Infer schema from the loaded data and generate type with properties
let xmlType (ownerType:TypeProviderForNamespaces) cfg =    
    let createType typeName (xmlText:string) =        
        let doc = XDocument.Parse xmlText
        createParserType<TypedXDocument> 
            typeName 
            (XmlInference.provideElement doc.Root.Name.LocalName [doc.Root])
            generateType
            (fun args -> <@@ TypedXDocument(XDocument.Parse xmlText) @@>)
            (fun args -> <@@ TypedXDocument(XDocument.Load(%%args.[0] : string)) @@>)
            (fun args -> <@@ TypedXElement((%%args.[0] : TypedXDocument).Document.Root) @@>)
    
    createStructuredParser "StructuredXml" cfg ownerType createType    