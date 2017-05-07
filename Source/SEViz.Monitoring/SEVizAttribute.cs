﻿/*
 * SEViz - Symbolic Execution VIsualiZation
 *
 * SEViz is a tool, which can support the test generation process by
 * visualizing the symbolic execution in a directed graph.
 *
 * Authors: Dávid Honfi <honfi@mit.bme.hu>, Zoltán Micskei
 * <micskeiz@mit.bme.hu>, András Vörös <vori@mit.bme.hu>
 * 
 * Copyright 2015 Budapest University of Technology and Economics (BME)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 * 
 */

using CFGlib;
using Microsoft.ExtendedReflection.ComponentModel;
using Microsoft.ExtendedReflection.Emit;
using Microsoft.ExtendedReflection.Interpretation;
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Metadata.Names;
using Microsoft.ExtendedReflection.Reasoning.ExecutionNodes;
using Microsoft.ExtendedReflection.Symbols;
using Microsoft.ExtendedReflection.Utilities.Safe.IO;
using Microsoft.Pex.Engine.ComponentModel;
using Microsoft.Pex.Engine.Packages;
using Microsoft.Pex.Engine.TestGeneration;
using Microsoft.Pex.Framework.ComponentModel;
using QuickGraph;
using SEViz.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SEViz.Monitoring
{
    public class SEVizAttribute : PexComponentElementDecoratorAttributeBase, IPexPathPackage, IPexExplorationPackage
    {
        #region Properties

        public string Name
        {
            get
            {
                return "SEViz";
            }
        }

        private Dictionary<int, CFGNode> Vertices
        {
            get; set;
        }

        private Dictionary<int, Dictionary<int, CFGEdge>> Edges
        {
            get; set;
        }

        public SoftMutableBidirectionalGraph<CFGNode, CFGEdge> Graph
        {
            get; private set;
        }


        private Dictionary<int, Tuple<bool, string>> EmittedTestResult
        {
            get; set;
        }

        private List<string> Z3CallLocations
        {
            get; set;
        }

        private Dictionary<int, Task<string>> PrettyPathConditionTasks
        {
            get; set;
        }

        private Dictionary<int, int> ParentNodes
        {
            get; set;
        }

        private string UnitNamespace
        {
            get; set;
        }

        private string DllPath
        {
            get; set;
        }

        private string FunctionName
        {
            get; set;
        }

        public CFGCreator MyCreator
        {
            get; set;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Visualizes symbolic execution.
        /// </summary>
        public SEVizAttribute ()
        {
            MessageBox.Show("YYYYYYYYYYYYYYYYYYYYYYYYYY");

        }

        //dll elérsi út + függvény
        /// <summary>
        /// Visualizes symbolic execution and marks edges that are pointing in and out of the unit borders.
        /// </summary>
        /// <param name="unitNamespace">The namespace of the unit to use.</param>
        public SEVizAttribute ( string unitNamespace )
        {
            MessageBox.Show("XXXXXXXXXXXXXXXXXXXXXX");
            UnitNamespace = unitNamespace;
        }


        public SEVizAttribute ( string dllPath, string functionName )
        {
            MessageBox.Show("CTOR");

            DllPath = dllPath;
            FunctionName = functionName;

            CFGCreator myCreator = new CFGCreator();
            myCreator.Create(DllPath, functionName);

            MyCreator = myCreator;

            foreach (var item in MyCreator.edges)
            {
                MessageBox.Show(item.Id.ToString());
            }

        }

        #endregion

        
        #region Exploration package

        public object BeforeExploration ( IPexExplorationComponent host )
        {

            MessageBox.Show("BeforeExploration");

            // Subscribing to constraint problem handler
            host.Log.ProblemHandler += ( problemEventArgs ) =>
            {
                var location = problemEventArgs.FlippedLocation.Method == null ?
                                "" :
                                ( problemEventArgs.FlippedLocation.Method.FullName + ":" + problemEventArgs.FlippedLocation.Offset );

                Z3CallLocations.Add(location);
            };

            // Subscribing to test emitting handler
            host.Log.GeneratedTestHandler += ( generatedTestEventArgs ) =>
            {
                // Getting the number of the corresponding run
                var run = generatedTestEventArgs.GeneratedTest.Run;

                // Storing the result of the test (this is called before AfterRun)
                EmittedTestResult.Add(run, new Tuple<bool, string>(generatedTestEventArgs.GeneratedTest.IsFailure, generatedTestEventArgs.GeneratedTest.MethodCode));
            };


            return null;
        }

        public void AfterExploration ( IPexExplorationComponent host, object data )
        {
            MessageBox.Show("AfterExploration");

            foreach (var vertex in Vertices.Values)
            {
                // Modifying shape based on Z3 calls
                if (Z3CallLocations.Contains(vertex.MethodName + ":" + vertex.ILOffset))
                    vertex.Shape = CFGNode.NodeShape.Ellipse;

                // Adding the path condition
                var t = PrettyPathConditionTasks [vertex.Id];
                t.Wait();
                vertex.PathCondition = t.Result;
            }

            foreach (var vertex in Vertices.Values)
            {
                // Adding the incremental path condition
                if (ParentNodes.ContainsKey(vertex.Id))
                {
                    vertex.IncrementalPathCondition = CalculateIncrementalPathCondition(vertex.PathCondition, Vertices [ParentNodes [vertex.Id]].PathCondition);
                }
                else
                {
                    // If the node is the first one (has no parents), then the incremental equals the full PC
                    vertex.IncrementalPathCondition = vertex.PathCondition;
                }
            }
            // +++
            // Adding vertices and edges to the graph
            Graph.AddVertexRange(Vertices.Values);
            foreach (var edgeDictionary in Edges.Values)
                Graph.AddEdgeRange(edgeDictionary.Values);



            //đ
            ////Graph.AddVertexRange(MyCreator.graph.Vertices);

            ////Graph.AddEdgeRange(MyCreator.graph.Edges);




            // Checking if temporary SEViz folder exists
            if (!Directory.Exists(Path.GetTempPath() + "SEViz"))
            {
                var dir = Directory.CreateDirectory(Path.GetTempPath() + "SEViz");

            }

            //đ
            //[SEViz(typeof(string).)]
            // Getting the temporary folder
            var tempDir = Path.GetTempPath() + "SEViz\\";

            // Serializing the graph into graphml
            CFGCreator.Serialize(Graph, tempDir);

        }

        #endregion

        #region Run package

        public object BeforeRun ( IPexPathComponent host )
        {
            MessageBox.Show("BeforeRun");
            return null;
        }

        public void AfterRun ( IPexPathComponent host, object data )
        {
            MessageBox.Show("AfterRun");

            // Getting the executions nodes in the current path
            var nodesInPath = host.PathServices.CurrentExecutionNodeProvider.ReversedNodes.Reverse().ToArray();

            // Getting the sequence id of the current run
            var runId = host.ExplorationServices.Driver.Runs;

            // Iterating over the nodes in the path
            foreach (var node in nodesInPath)
            {
                var vertex = new CFGNode(node.UniqueIndex, false);

                // Adding the method name this early in order to color edges
                string methodName = null;
                int offset = 0;
                if (node.CodeLocation.Method == null)
                {
                    if (node.InCodeBranch.Method != null)
                    {
                        methodName = node.InCodeBranch.Method.FullName;
                    }
                }
                else
                {
                    methodName = node.CodeLocation.Method.FullName;
                    offset = node.CodeLocation.Offset;
                }
                // Setting the method name
                vertex.MethodName = methodName;

                // Setting the offset
                vertex.ILOffset = (uint)offset;

                var nodeIndex = nodesInPath.ToList().IndexOf(node);
                if (nodeIndex > 0)
                {
                    var prevNode = nodesInPath [nodeIndex - 1];
                    // If there is no edge between the previous and the current node
                    if (!( Edges.ContainsKey(prevNode.UniqueIndex) && Edges [prevNode.UniqueIndex].ContainsKey(node.UniqueIndex) ))
                    {

                        var prevVertex = Vertices [prevNode.UniqueIndex];

                        var edge = new CFGEdge(new Random().Next(), prevVertex, vertex);

                        Dictionary<int, CFGEdge> outEdges = null;
                        if (Edges.TryGetValue(prevNode.UniqueIndex, out outEdges))
                        {
                            outEdges.Add(node.UniqueIndex, edge);
                        }
                        else
                        {
                            Edges.Add(prevNode.UniqueIndex, new Dictionary<int, CFGEdge>());
                            Edges [prevNode.UniqueIndex].Add(node.UniqueIndex, edge);
                        }

                        // Edge coloring based on unit border detection
                        if (UnitNamespace != null)
                        {
                            // Checking if pointing into the unit from outside
                            if (!( prevVertex.MethodName.StartsWith(UnitNamespace + ".") || prevVertex.MethodName.Equals(UnitNamespace) ) && ( vertex.MethodName.StartsWith(UnitNamespace + ".") || vertex.MethodName.Equals(UnitNamespace) ))
                            {
                                edge.Color = CFGEdge.EdgeColor.Green;
                            }

                            // Checking if pointing outside the unit from inside
                            if (( prevVertex.MethodName.StartsWith(UnitNamespace + ".") || prevVertex.MethodName.Equals(UnitNamespace) ) && !( vertex.MethodName.StartsWith(UnitNamespace + ".") || vertex.MethodName.Equals(UnitNamespace) ))
                            {
                                edge.Color = CFGEdge.EdgeColor.Red;
                            }
                        }
                    }
                }

                // If the node is new then it is added to the list and the metadata is filled
                if (!Vertices.ContainsKey(node.UniqueIndex))
                {

                    Vertices.Add(node.UniqueIndex, vertex);

                    // Adding source code mapping
                    vertex.SourceCodeMappingString = MapToSourceCodeLocationString(host, node);

                    // Setting the border based on mapping existence
                    vertex.Border = vertex.SourceCodeMappingString == null ? CFGNode.NodeBorder.Single : CFGNode.NodeBorder.Double;

                    // Setting the color
                    if (nodesInPath.LastOrDefault() == node)
                    {
                        if (!EmittedTestResult.ContainsKey(runId))
                        {
                            vertex.Color = CFGNode.NodeColor.Orange;
                        }
                        else
                        {
                            if (EmittedTestResult [runId].Item1)
                            {
                                vertex.Color = CFGNode.NodeColor.Red;
                            }
                            else
                            {
                                vertex.Color = CFGNode.NodeColor.Green;
                            }
                            vertex.GenerateTestCode = EmittedTestResult [runId].Item2;
                        }
                    }
                    else
                    {
                        vertex.Color = CFGNode.NodeColor.White;
                    }

                    // Setting the default shape
                    vertex.Shape = CFGNode.NodeShape.Rectangle;

                    // Adding path condition tasks and getting the required services
                    TermEmitter termEmitter = new TermEmitter(host.GetService<TermManager>());
                    SafeStringWriter safeStringWriter = new SafeStringWriter();
                    IMethodBodyWriter methodBodyWriter = host.GetService<IPexTestManager>().Language.CreateBodyWriter(safeStringWriter, VisibilityContext.Private, 2000);
                    PrettyPathConditionTasks.Add(vertex.Id, PrettyPrintPathCondition(termEmitter, methodBodyWriter, safeStringWriter, node));

                    // Setting the status
                    vertex.Status = node.ExhaustedReason.ToString();

                    // Collecting the parent nodes for the later incremental path condition calculation
                    if (nodeIndex > 0)
                    {
                        ParentNodes.Add(vertex.Id, nodesInPath [nodeIndex - 1].UniqueIndex);
                    }
                }

                // Adding the Id of the run
                Vertices [node.UniqueIndex].Runs += ( runId + ";" );
            }
        }

        #endregion

        public void Initialize ( IPexExplorationEngine host )
        {
            MessageBox.Show("INITALIZE()");

            Graph = new SoftMutableBidirectionalGraph<CFGNode, CFGEdge>();
            Vertices = new Dictionary<int, CFGNode>();
            Edges = new Dictionary<int, Dictionary<int, CFGEdge>>();
            EmittedTestResult = new Dictionary<int, Tuple<bool, string>>();
            Z3CallLocations = new List<string>();
            PrettyPathConditionTasks = new Dictionary<int, Task<string>>();
            ParentNodes = new Dictionary<int, int>();
        }


        //đ másik az original
        ////////public void Initialize ( IPexExplorationEngine host )
        ////////{
        ////////    Graph = MyCreator.graph;
        ////////    Vertices = new Dictionary<int, CFGNode>();

        ////////    foreach (var item in Graph.Vertices)
        ////////    {
        ////////        Vertices.Add(item.Id, item);

        ////////    }

        ////////    Edges = new Dictionary<int, Dictionary<int, CFGEdge>>();

        ////////    foreach (var item in Graph.Edges)
        ////////    {
        ////////        var tempdict = new Dictionary<int, CFGEdge>();
        ////////        tempdict.Add(item.Source.Id, item);
        ////////        Edges.Add(item.Id, tempdict);

        ////////    }
        ////////    EmittedTestResult = new Dictionary<int, Tuple<bool, string>>();
        ////////    Z3CallLocations = new List<string>();
        ////////    PrettyPathConditionTasks = new Dictionary<int, Task<string>>();
        ////////    ParentNodes = new Dictionary<int, int>();
        ////////}
        public void Load ( IContainer pathContainer )
        {
            MessageBox.Show("LOAD()");
        }

        protected override void Decorate ( Name location, IPexDecoratedComponentElement host )
        {
            MessageBox.Show("DECORATE()");

            host.AddExplorationPackage(location, this);
            host.AddPathPackage(location, this);
        }

        #region Private methods 

        /// <summary>
        /// Maps the execution node to source code location string.
        /// </summary>
        /// <param name="host">Host of the Pex Path Component</param>
        /// <param name="node">The execution node to map</param>
        /// <returns>The source code location string in the form of [documentlocation]:[linenumber]</returns>
        private string MapToSourceCodeLocationString ( IPexPathComponent host, IExecutionNode node )
        {
            MessageBox.Show("MapToSourceCodeLocationString()");
           
            try
            {
                var symbolManager = host.GetService<ISymbolManager>();
                var sourceManager = host.GetService<ISourceManager>();
                MethodDefinitionBodyInstrumentationInfo nfo;
                if (node.CodeLocation.Method == null)
                {
                    if (node.InCodeBranch.Method == null)
                    {
                        return null;
                    }
                    else
                    {
                        node.InCodeBranch.Method.TryGetBodyInstrumentationInfo(out nfo);
                    }
                }
                else
                {
                    node.CodeLocation.Method.TryGetBodyInstrumentationInfo(out nfo);
                }
                SequencePoint point;
                int targetOffset;
                nfo.TryGetTargetOffset(node.InCodeBranch.BranchLabel, out targetOffset);
                if (symbolManager.TryGetSequencePoint(node.CodeLocation.Method == null ? node.InCodeBranch.Method : node.CodeLocation.Method, node.CodeLocation.Method == null ? targetOffset : node.CodeLocation.Offset, out point))
                {
                    return point.Document + ":" + point.Line;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                // TODO Exception handling
                return null;
            }
        }

        /// <summary>
        /// Pretty prints the path condition of the execution node.
        /// </summary>
        /// <param name="host">Host of the Pex Path Component</param>
        /// <param name="node">The execution node to map</param>
        /// <returns>The pretty printed path condition string</returns>
        private Task<string> PrettyPrintPathCondition ( TermEmitter emitter, IMethodBodyWriter mbw, SafeStringWriter ssw, IExecutionNode node )
        {
            MessageBox.Show("PrettyPrintPathCondition()");
            var task = Task.Factory.StartNew(() =>
            {
                string output = "";
                try
                {
                    if (node.GetPathCondition().Conjuncts.Count != 0)
                    {

                        if (emitter.TryEvaluate(node.GetPathCondition().Conjuncts, 2000, mbw)) // TODO Perf leak
                        {
                            for (int i = 0; i < node.GetPathCondition().Conjuncts.Count - 1; i++)
                            {
                                mbw.ShortCircuitAnd();
                            }

                            mbw.Statement();
                            var safeString = ssw.ToString();
                            output = safeString.Remove(ssw.Length - 3);
                        }
                    }
                }
                catch (Exception) { }
                return output;
            });
            return task;

        }

        /// <summary>
        /// Calculates the incremental path condition of a node
        /// </summary>
        /// <param name="pc">The path condition of the current node</param>
        /// <param name="prevPc">The path condition of the previous node</param>
        /// <returns>The incremental path condition of the node</returns>
        private string CalculateIncrementalPathCondition ( string pc, string prevPc )
        {
            MessageBox.Show("CalculateIncrementalPathCondition()");


            var remainedLiterals = new List<string>();

            var splittedCondition = pc.Split(new string [] { "&& " }, StringSplitOptions.None);
            var prevSplittedCondition = prevPc.Split(new string [] { "&& " }, StringSplitOptions.None);

            var currentOrdered = splittedCondition.OrderBy(c => c);
            var prevOrdered = prevSplittedCondition.OrderBy(c => c);

            remainedLiterals = currentOrdered.Except(prevOrdered).ToList(); // TODO Perf leak

            Parallel.For(1, 3, ( i ) =>
            {
                Parallel.ForEach(prevOrdered, c =>
                {
                    var incrementedLiteral = Regex.Replace(c, "s\\d+", n => "s" + ( int.Parse(n.Value.TrimStart('s')) + i ).ToString());
                    remainedLiterals.Remove(incrementedLiteral);
                });
            });

            var remainedBuilder = new StringBuilder();
            foreach (var literal in remainedLiterals)
            {
                if (remainedLiterals.IndexOf(literal) != 0)
                    remainedBuilder.Append(" && ");
                remainedBuilder.Append(literal);
            }

            return remainedBuilder.ToString();
        }



        #endregion
    }
}
