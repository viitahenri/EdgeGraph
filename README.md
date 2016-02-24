# EdgeGraph

This is a Unity editor tool made for my bachelor's thesis, commissioned by Mental Moustache Ltd.

The tool's basic purpose is to generate sub edges inside areas determined by the user. The user creates nodes and edges that are processed into primitives using minimal cycle extraction. Sub edges are then generated inside the primitives using space colonization.

Detailed description of the tool is included in the thesis report found [here](http://urn.fi/URN:NBN:fi:amk-201602232560).

**The project includes the EdgeGraph tool (explained below) and a general L-System.**

## Installation

Add the EdgeGraph folder or the L-System folder into your project, which ever you wish to utilize.

## Usage

**Here is the general explanation for using EdgeGraph's editor. L-system will not be explained further.**

### Inspector

In the EdgeGraph’s Inspector, the user can manipulate the node and edge data, control the generation process and manipulate the parameters. The sub edge generation parameters and controls are hidden if there are no primitives in the graph.

The sub edge generation parameters in the inspector are as follows:
-	Target count is the amount of points generated inside the primitive.
-	Margin is the minimum distance that the generated points are al-lowed to be from the primitive edges.
-	Width is the edge width that is set to every sub edge.
-	Min Angle is the minimum angle the edge builder can turn on one iteration. Smaller angles result in a 90-degree turn.
-	Segment Length is the distance the edge builder advances in each iteration.
-	Min Distance is the minimum distance from current position on edge builder to generated points at which the point is considered visited.
-	Max Distance is the maximum distance at which the edge builder considers the generated points towards which to advance. Closest point is always chosen among ones at less than max distance.
-	Sub node combine range is the range within which sub nodes are combined. The generation results are better when the range is more than zero as the resulting nodes are not very close to each other.
-	Sub node end connection range is the range within which ending nodes (nodes with one adjacent node) are connected to other nodes in order to ensure full primitives in the result.

### Scene View

#### Nodes
The position modification of nodes is made by moving the nodes in the XZ –plane , so when the user drags a node’s cube handle, only the node position’s X and Z values change. The existing nodes can be removed by holding shift key and selecting a node. New nodes can be added in two ways: *adding a node to cursor position* and *adding a node by splitting an edge*. First is done by holding control key, and the second is done by holding both shift and control keys. When the new node is being added to an existing edge, an indicator (blue dot) is drawn on the closest point on the closest edge to the cursor, where the new node will be created.

#### Edges
The handle functions used to indicate the edges are lines, and when in edge editing mode the cubes indicating nodes cannot be moved. The node cubes are used when new edges are created. The user starts by clicking a node they want the edge to start from and simultaneously pressing control key, and drag towards other nodes. The tool will draw a differently coloured line to the closest node from the cursor, indicating where the new edge will be created if the user lets go of the mouse button. If the closest node is the node where the user started, no line will be drawn and this way the user can cancel the new edge adding. In order to remove existing edges, when shift key is pressed cubes are drawn in the middle of the edges and by clicking these the edges are removed.

Edge widths can be edited in the edge editing mode by enabling a toggle. When in width editing mode, the user can use a brush-like tool to help change widths of several edges easily, or change width of every edge to the set value.

#### Primitives
When the user has processed the minimal cycles by pressing the button in the inspector, the primitive mode is enabled on the scene view. In the primitive mode, the user can select one or more primitives that were found on the graph by holding control key and clicking inside the primitives and change the generation parameters for the selected primitives. If none is selected, the settings are set to every primitive. If the user holds shift key in the primitive mode, the root node selection tool is enabled. The tool will indicate the closest node of the selected primitive from the cursor, and by clicking the user sets the node. The root node is the node from which the sub edge generation starts.

#### Sub edges
After setting the parameters, the sub edges can be generated either by using the current seed visible in the UI or by generating or inputting a new random seed before the generation. When the sub edges are generated, the primitive editing mode will show the generated sub edges by drawing a line handles for the sub edges, and cube handles for the nodes that the sub edges go through.

## License

See LICENSE.
