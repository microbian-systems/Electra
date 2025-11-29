# Implemented Tree Data Structures

This project contains C# implementations for a variety of common tree data structures.

## Implemented Trees

*   **Binary Tree**: A tree data structure in which each node has at most two children.
*   **Binary Search Tree (BST)**: A binary tree where the left child contains a key less than the parent and the right child contains a key greater than the parent.
*   **AVL Tree**: A self-balancing binary search tree.
*   **Red-Black Tree**: A self-balancing binary search tree that uses node coloring to ensure balance.
*   **Splay Tree**: A self-balancing binary search tree that moves frequently accessed elements closer to the root.
*   **Treap**: A randomized binary search tree that uses both a key and a priority.
*   **Binary Heap**: A complete binary tree that satisfies the heap property, useful for priority queues.
*   **B-Tree**: A self-balancing tree that is optimized for systems that read and write large blocks of data.
*   **B+ Tree**: A variation of the B-Tree where all data is stored in the leaf nodes.
*   **Trie**: A tree used for storing a dynamic set of strings, for fast prefix searches.
*   **Segment Tree**: A tree data structure for storing information about intervals or segments.
*   **Interval Tree**: A tree designed to hold intervals and allows for efficient searching of intervals that overlap with a given interval.
*   **KD-Tree**: A space-partitioning data structure for organizing points in a k-dimensional space.
*   **R-Tree**: A tree data structure used for storing spatial data. (Structural implementation)
*   **Expression Tree / AST**: A tree that represents the structure of a mathematical or logical expression.

## Search Algorithms

The following search algorithms are implemented and can be used with the appropriate tree structures:

*   **Depth-First Search (DFS)**: Preorder, Inorder, and Postorder traversals.
*   **Breadth-First Search (BFS)**: Level-order traversal.

## Examples

For examples on how to use these data structures, please see the [Program.cs](./Examples/Program.cs) file.

---

#  Common Tree Data Structures in Computer Science

## 1. Binary Trees (Foundational)
- **Binary Tree**  each node has up to two children.
- **Full Binary Tree**  every node has 0 or 2 children.
- **Complete Binary Tree**  levels filled left to right.
- **Perfect Binary Tree**  full and complete; all leaves at the same level.

---

## 2. Binary Search Trees (BSTs)
- **Binary Search Tree (BST)**  basic sorted tree, can become unbalanced.
- **AVL Tree**  strict height balancing.
- **RedBlack Tree**  widely used, easier to maintain balance.
- **Splay Tree**  frequently accessed items bubble to the root.
- **Treap**  randomized BST with heap properties.
- **Scapegoat Tree**  rebuilds subtrees for balance.
- **Tango Tree / Link-Cut Tree**  advanced dynamic BST variants.

---

## 3. Heaps (Priority Trees)
- **Binary Heap**  basis of priority queues.
- **Binomial Heap**  supports efficient merges.
- **Fibonacci Heap**  optimal theoretical amortized times.
- **Pairing Heap**  simpler, practical alternative to Fibonacci heaps.

---

## 4. B-Trees and Variants (Disk/Storage Optimized)
- **B-Tree**  m-ary balanced tree for disks and SSD-based indexing.
- **B+ Tree**  all data stored at leaves; dominant DB index structure.
- **B\* Tree**  higher node utilization.
- **Blink Tree**  sibling-linked for concurrency control.

---

## 5. Tries (Prefix Trees)
- **Trie**  character-by-character prefix lookup.
- **Radix / Patricia Trie**  compressed memory-efficient trie.
- **Suffix Tree**  substring search in linear time.
- **DAWG**  compact directed acyclic word graph for lexicons.

---

## 6. Segment & Interval Trees
- **Segment Tree**  range queries (sum/min/max).
- **Fenwick Tree (BIT)**  efficient prefix sums.
- **Interval Tree**  fast overlap queries.
- **Range Tree**  multi-dimensional range queries.

---

## 7. Spatial / Geometric Trees
- **Quadtree**  2D partitioning.
- **Octree**  3D partitioning.
- **KD-Tree**  k-dimensional nearest neighbor search.
- **R-Tree**  spatial indexing.
- **R\*-Tree**  optimized R-tree with minimal overlap.

---

## 8. Dynamic Trees (Graph Algorithms)
- **Link-Cut Tree**  dynamic connectivity.
- **Euler Tour Tree**  maintains dynamic forests.
- **Top Tree**  advanced dynamic connectivity structure.

---

## 9. Specialized Trees
- **AA Tree**  simplified redblack tree.
- **Suffix Automaton**  DAG used like a compact suffix index.
- **Expression Tree**  used in parsers/interpreters.
- **Decision Tree**  used in machine learning.
- **AST (Abstract Syntax Tree)**  compiler structure.

---

#  Most Important in Real-World Systems Today

## 1. RedBlack Tree
Used in:
- C++ `std::map`, `std::set`
- Java `TreeMap`
- Linux kernel data structures

## 2. B+ Tree
Used in:
- MySQL, PostgreSQL, SQLite indexes
- Filesystems (NTFS, HFS+, APFS, EXT4)

## 3. Trie / Radix Tree
Used in:
- IP routing tables
- Redis internal structures
- Full-text search engines

## 4. Binary Heap
Used in:
- Priority queues
- OS schedulers
- Dijkstras algorithm

## 5. Segment Tree / Fenwick Tree
Used in:
- Range queries
- Competitive programming
- Some game engines and simulations

## 6. KD-Tree / R-Tree
Used in:
- Machine learning (nearest-neighbor search)
- GIS spatial indexing
- Game and physics engines

---

#  BFS vs DFS on Tree Data Structures

BFS and DFS arent tied to specific tree *types*they are **traversal strategies** used depending on the *goal of the search*, *memory constraints*, and *structure of the data*.  
Certain trees and problems naturally favor one over the other.

---

#  When BFS Is Used on Trees

BFS explores **level by level**.  
You use BFS when:

###  1. Shortest Path
BFS guarantees minimal edge distance first.

Examples:
- Find the nearest ancestor containing X
- Shortest number of operations to reach a node

###  2. Level-Order Processing
Common in:
- Heaps (must be level-complete)
- Balanced-tree height checks
- Serialization formats (array representation)

###  3. Broad/Shallow Trees
- Tries for autocomplete suggestions
- File-system directory trees
- B-Trees, B+ Trees (large fan-out)

**BFS is naturally applied to:**
- Binary heaps
- Tries
- Decision trees
- B-Trees

---

#  When DFS Is Used on Trees

DFS dives deep along a branch before backtracking.

### DFS Variants:
- **Preorder**  Node  Left  Right
- **Inorder**  Left  Node  Right
- **Postorder**  Left  Right  Node

###  1. Sorted Data Traversal
Example: Inorder traversal on a BST yields sorted output.

Used with:
- BST
- AVL Trees
- RedBlack Trees

###  2. Structural Operations
- Deleting subtrees
- Cloning trees
- Evaluating expressions (Expression Trees / ASTs)
- Serializing/deserializing trees recursively
- Computing tree height

###  3. Memory Efficiency
- DFS uses O(h) memory (h = tree height)  
- BFS uses O(n) memory (n = number of nodes)

**DFS is naturally applied to:**
- ASTs
- Expression Trees
- Segment Trees
- Interval Trees
- KD-Trees, R-Trees
- Suffix Trees

---

#  When Both Are Used

### Tries
- BFS: autocomplete suggestions (shorter words first)  
- DFS: dictionary dump (list all words in sorted order)

### B+ Trees
- BFS: index traversal through internal nodes  
- DFS: leaf chain scanning

### R-Trees / Spatial Trees
- BFS: broad collision detection  
- DFS: precise query refinement

---

#  Quick Summary Table

| Tree Type | BFS Use Cases | DFS Use Cases |
|----------|----------------|----------------|
| **BST / AVL / RedBlack** | Level checks, completeness | Sorted traversal, insert/delete |
| **Heap (Binary Heap)** | Always BFS for shape | Rarely DFS |
| **Trie** | Autocomplete, prefix search | Listing words, deep pattern search |
| **B-Tree / B+ Tree** | DB index node traversal | Leaf scans, range scans |
| **Segment Tree** | Rare | Range queries, updates |
| **Interval Tree** | Rare | Interval overlap search |
| **KD-Tree** | K-NN breadth search | Best-fit deep search |
| **R-Tree** | Broad-phase collision | Narrow-phase collision |
| **AST / Expression Tree** | None | Evaluation, compilation |

---

#  Rule of Thumb

**Use BFS when:**
- Shortest path / nearest match needed
- Level-based processing
- Wide/fat trees (B-Trees, Trie)

**Use DFS when:**
- Sorted output needed
- Recursive operations
- Memory efficiency important (deep trees)
- Traversing compiler/language trees (AST, expression tree)