# COP5615 Distributed Operating System Principles Project 3

## Chord-P2P-System-And-Simulation-Actor-Model-F#-Akka.Net

## Project Description

You have to implement the network join and routing as described in the Chord paper (Section 4) and encode the simple application that associates a key (same as the ids used in Chord) with a string. You can change the message type sent and the specific activity as long as you implement it using a similar API to the one described in the paper. <br>
Link: https://pdos.csail.mit.edu/papers/ton:chord/paper-ton.pdf

## Project Requirements

### Input

The input provided (as command line) will be of the form: numberOfNodes numberOfRequests Where numberOfNodes is the number of peers to be created in the peer-to-peer system and numberOfRequests is the number of requests each peer has to make. When all peers performed that many requests, the program can exit. Each peer should send a request/second.

### Actor modeling

In this project, you have to use exclusively the AKKA actor framework (projects that do not use multiple actors or use any other form of parallelism will receive no credit).  You should have one actor for each of the peers modeled.

### Output

Print the average number of hops (node connections) that have to be traversed to deliver a message.

### Example:

dotnet fsi Project3.fsx 2500 100 <br>
Average number of hops (node connections) that have to be traversed to deliver a message : 5.295244

## Submitted By:

Name: Parth Gupta, UFID: 91997064

## What is Working?

- 

## The largest network that I managed to deal with:

| Number of Nodes | Number of Requests | Avg Number of Hops |
| --- | --- | --- |
|  |  |  |
|  |  |  |
|  |  |  | 
|  |  |  |

## Built On

- Programming language: F# 
- Framework: AKKA.NET
- Operating System: Windows 10
- Programming Tool: Visual Studio Code
