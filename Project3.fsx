#r "nuget: Akka.FSharp"

#load "helper.fsx"

open Declaration

open System
open Akka.FSharp
open Akka.Actor

let system = ActorSystem.Create("ChordAlgorithm", Configuration.defaultConfig())

let Peers (mailbox: Actor<_>) =

    let mutable Key = 0;
    let mutable successor = {NodeKey = -1; NodeRef = mailbox.Self};
    let mutable preceding = {NodeKey = -1; NodeRef = mailbox.Self};
    let mutable routingTable = [];
    let mutable routingArray = [||];

    let nearestPreviousNode id =

        let mutable check = -1;
        let mutable newID = -1;
        let mutable newKeyVal = -1;
        let mutable i = maxNodes - 1;

        while i >= 0 do
        
            if routingTable.[i].NodeKey >= Key then
                newKeyVal <- routingTable.[i].NodeKey;
            else 
                newKeyVal <- routingTable.[i].NodeKey + maxNumberOfNodes;

            if id >= Key then
                newID <- id;
            else 
                newID <- id + maxNumberOfNodes;

            if check = -1 && newKeyVal > Key && newKeyVal < newID then
                check <- i;

            i <- i - 1;

        if check = -1 then
            mailbox.Self
        else 
            routingTable.[check].NodeRef
        


    let rec loop() = actor {
        let! message = mailbox.Receive()
        match box message with
        | :? int as id -> 
            let target = nearestPreviousNode id
            if (target) <> mailbox.Self then
                target <! id

        | :? queryMessage as qm ->
            let target = nearestPreviousNode qm.Key
            if (target) <> mailbox.Self then
                target <! {Key = qm.Key; Hop = qm.Hop + 1; Source = qm.Source}
            else
                qm.Source <! qm

        | :? NodeSetupInfo as nsi ->
            Key <- nsi.NodeSetupKey
            successor <- nsi.NextNodeInfo
            preceding <- nsi.PreviousNodeInfo
            routingTable <- nsi.NodeTable

        | :? NodeInfo as ni ->
            let target = nearestPreviousNode ni.NodeKey
            if target <> mailbox.Self then
                target <! ni
            else
                let previousNode = {NodeKey = Key; NodeRef = mailbox.Self}
                let nextNode = {NodeKey = successor.NodeKey; NodeRef = successor.NodeRef}
                let nodeInfo = {PreviousNode = previousNode; NextNode = nextNode}
                ni.NodeRef <! nodeInfo
                successor.NodeRef <! {UpdatedPreviousNode = ni}
                successor <- ni
                let mutable i = 0;
                while i <= (maxNodes - 1) do
                    let power = Math.Pow((float) 2, (float) i) |> int
                    let nodeRefInfo = {TableKey = Key + power; SourceIndex = i; SourceNode = mailbox.Self; IsReturn = false; 
                                        DestinationIndex = -1; DestinationNode = mailbox.Self} 
                    successor.NodeRef <! nodeRefInfo
                    i <- i + 1;

        | :? UpdatePreviousNode as upn ->
            preceding <- upn.UpdatedPreviousNode

        | :? NodeJoin as nj ->
            preceding <- nj.PreviousNode
            successor <- nj.NextNode
            let mutable i = 0;
            while i <= (maxNodes - 1) do
                let power = Math.Pow((float) 2, (float) i) |> int
                let nodeRefInfo = {TableKey = Key + power; SourceIndex = i; SourceNode = mailbox.Self; IsReturn = false; 
                                    DestinationIndex = -1; DestinationNode = mailbox.Self} 
                successor.NodeRef <! nodeRefInfo
                i <- i + 1;         
        
        | :? RoutingTable as rt ->
            if rt.IsReturn = false then
                let target = nearestPreviousNode rt.TableKey
                if target <> mailbox.Self then
                    target <! rt
                else
                    let sourceNodeInfo = {SourceNode = rt.SourceNode; TableKey = rt.TableKey; SourceIndex = rt.SourceIndex; IsReturn = true; 
                                           DestinationIndex = successor.NodeKey; DestinationNode = successor.NodeRef}
                    rt.SourceNode <! sourceNodeInfo
            else
                Array.set routingArray rt.SourceIndex {NodeKey = rt.DestinationIndex; NodeRef = rt.DestinationNode}

        | _ -> ()
            
        return! loop()
    }
    loop()

let actorList = [
    let mutable i = 1;
    while i <= numberOfNodes do
        let actorName = "Actor" + i.ToString()
        yield (spawn system actorName Peers)
        i <- i + 1;
]

let inputListOfRequestLists = [
    let mutable i = 1;
    while i <= numberOfNodes do
        let requestList = [
            let mutable j = 1;
            while j <= numberOfRequests do
                let randomNum = (random.Next() % 100) + 1
                let str = getString randomNum
                j <- j + 1;
                yield str
        ]
        i <- i + 1;
        yield requestList
]

let randomNodeList = [
    let mutable i = 1;
    while i <= numberOfNodes do
        yield (random.Next() % maxNumberOfNodes)
        i <- i + 1;
]

let numList = List.sort randomNodeList

let mark (idx : int) = 
    let mutable value = -1;
    if idx < numberOfNodes then
        value <- numList.[idx % numberOfNodes]
    else 
        value <- numList.[idx % numberOfNodes] + maxNumberOfNodes;
    value

let mutable i = 0;
while i <= (numberOfNodes - 1) do
    let nodeList = [
        let mutable j = 0;
        while j <= (maxNodes - 1) do
           let power = Math.Pow((float) 2, (float) j) |> int
           let mutable visited = -1; 
           let mutable k = 1;
           while k <= numberOfNodes do
               let idx1 = i + k
               let value = mark idx1
               if value >= power + numList.[i] && visited = -1 then
                   visited <- idx1 % numberOfNodes;
               k <- k + 1;
           let currNodeInfo = {NodeKey = numList.[visited]; NodeRef = actorList.[visited]}
           j <- j + 1;
           yield currNodeInfo
    ]
    let idx2 = i + 1    
    let nextNodeInfo = {NodeKey = numList.[idx2 % numberOfNodes]; NodeRef = actorList.[idx2 % numberOfNodes]}
    let idx3 = i + numberOfNodes - 1
    let previousNodeInfo = {NodeKey = numList.[idx3 % numberOfNodes]; NodeRef = actorList.[idx3 % numberOfNodes]}
    let completeNodeInfo = {NodeSetupKey = numList.[i]; NextNodeInfo = nextNodeInfo; PreviousNodeInfo = previousNodeInfo; NodeTable = nodeList}
    actorList.[i] <! completeNodeInfo
    i <- i + 1;

let BossActor = 
    spawn system "BossActor" 
        (fun mailbox ->
            let mutable count = 0
            let mutable hops = 0
            let rec loop() = actor {
                let! message = mailbox.Receive()
                match box message with
                | :? string as str ->
                    if str = "StartingChordAlgorithm" then
                        let mutable i = 0;
                        while i <= (numberOfNodes - 1) do
                            let mutable j = 0;
                            while j <= (numberOfRequests - 1) do
                                let hash = generateHash inputListOfRequestLists.[i].[j]
                                actorList.[i] <! {Key = hash % maxNumberOfNodes; Hop = 0; Source = mailbox.Self}
                                j <- j + 1;
                            i <- i + 1;

                | :? queryMessage as qm ->
                    hops <- hops + qm.Hop
                    count <- count + 1
                    if count = numberOfNodes * numberOfRequests then
                        printfn "Average number of hops (node connections) that have to be traversed to deliver a message : %f" ((float) hops / (float) count)
                        Environment.Exit(1);

                | _ -> ()

                return! loop()
            }
            loop()
        )


BossActor <! "StartingChordAlgorithm"

System.Console.ReadLine() |> ignore

0
