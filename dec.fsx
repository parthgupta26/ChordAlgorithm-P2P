// declaring a module so that I can import this module in different script file
module Declaration

#r "nuget: Akka.FSharp"

open System
open Akka.Actor

// structre to store the current node information
// it will be uniquely identified with the help of the NodeKey
[<Struct>]
type NodeInfo = {
        NodeKey: int
        NodeRef: IActorRef
    }

// each node n maintains a routing table with up to m entries
// this routing table is known as finger table
[<Struct>]
type RoutingTable = {
        TableKey: int
        SourceIndex: int
        SourceNode: IActorRef
        IsReturn: bool
        DestinationIndex: int
        DestinationNode: IActorRef
    }

// setting up the node info based on previous and next node    
[<Struct>]
type NodeSetupInfo = {
        NodeSetupKey: int
        NextNodeInfo: NodeInfo
        PreviousNodeInfo: NodeInfo
        NodeTable: List<NodeInfo>
    }

[<Struct>]
type NodeJoin = {
        PreviousNode: NodeInfo
        NextNode: NodeInfo
    }

[<Struct>]
type UpdatePreviousNode = {
        UpdatedPreviousNode: NodeInfo
    }

[<Struct>]
type queryMessage = {
        Source: IActorRef
        Key: int
        Hop: int
    }

// starting of the program / main entry
// taking all the required arguments from the command line
// starting with index 1 since index 0 will have the file name
let argv = fsi.CommandLineArgs
let numberOfNodes = argv.[1] |> int
let numberOfRequests = argv.[2] |> int

let mutable maxNodes = 0;
let mutable nodes = numberOfNodes;

// calculating the nearest power of two
while nodes > 0 do
    maxNodes <- maxNodes + 1
    nodes <- (nodes / 2)  

let maxNumberOfNodes = Math.Pow((float) 2, (float) maxNodes) |> int

// concistent hashing
// consistent hashing tends to balance load,
// since each node receives roughly the same number of keys, and
// requires relatively little movement of keys when nodes join and
// leave the system
let generateHash (str : string) =
    // let chars = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
    let value = System.Text.Encoding.ASCII.GetBytes(str)
    let computedHash = System.Security.Cryptography.SHA1.Create().ComputeHash(value)
    let mutable result = 0;
    let mutable i = 0;
    while i <= (computedHash.Length - 1) do
        result <- result + (computedHash.[i] |> int);
        i <- i + 1;
    abs result