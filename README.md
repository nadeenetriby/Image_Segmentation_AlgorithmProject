# Image Segmentation Project
This project implements a graph-based image segmentation technique. It converts an image into a weighted undirected graph and segments it into distinct regions based on internal consistency and boundary evidence. The approach follows the method proposed in “Efficient Graph-Based Image Segmentation” by Felzenszwalb and Huttenlocher.

## Overview

Image segmentation is a fundamental task in computer vision that partitions an image into meaningful regions. This project transforms an image into a graph, assigns edge weights based on pixel intensity differences, and applies a region-merging algorithm to group visually similar pixels.

Key principles:
- **Internal Coherence**: Pixels inside a region are visually similar.
- **Boundary Significance**: Neighboring regions differ significantly.

---

## Project Features

-Graph representation of images using 8-connected pixel neighborhoods.
-Edge weights based on pixel intensity or RGB channel differences.
-Efficient image segmentation using Kruskal’s algorithm combined with Breadth-First Search (BFS) for connected components, as described by Felzenszwalb and Huttenlocher.
-Segmentation visualization with distinct region colors.
-Outputs the number and size of regions to a text file.
-Supports grayscale and color images.
-Optional Gaussian filter for noise reduction.


##**Bonus Features**

— Interactive “Click-to-Merge” UI

Enables manual merging of over-segmented regions by clicking on them through a graphical interface.
