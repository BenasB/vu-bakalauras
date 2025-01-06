const svg = d3
  .select("#tree")
  .append("svg")
  .attr("width", "100%")
  .attr("height", "100%");

let currentTransform = { x: window.innerWidth / 6, y: window.innerHeight / 2 };
let selectedNode = null;

const g = svg
  .append("g")
  .attr("transform", `translate(${currentTransform.x}, ${currentTransform.y})`);

const zoom = d3
  .zoom()
  .scaleExtent([0.15, 1.5])
  .on("zoom", (event) => {
    g.attr("transform", event.transform);
  })
  .on("start", () => {
    svg.style("cursor", "grabbing");
  })
  .on("end", () => {
    svg.style("cursor", "grab");
  });

svg.call(zoom);
svg.call(
  zoom.transform,
  d3.zoomIdentity.translate(currentTransform.x, currentTransform.y)
);

async function loadAndRenderTree() {
  let treeData;
  try {
    const response = await fetch("data.json");
    treeData = await response.json();
  } catch (error) {
    console.error("Error loading JSON:", error);
  }

  const root = d3.hierarchy(treeData, (d) => d.Children);
  const treeLayout = d3.tree().nodeSize([60, 350]);
  treeLayout(root);

  // Links
  const linkData = root.links();
  linkData.forEach((link) => {
    link.target.linkToParent = link; // Add reference to the link on the target node
  });

  const links = g
    .selectAll(".link-group")
    .data(linkData)
    .enter()
    .append("g")
    .attr("class", "link-group");

  links
    .append("path")
    .attr("class", "link")
    .attr(
      "d",
      d3
        .linkHorizontal()
        .x((d) => d.y)
        .y((d) => d.x)
    )
    .each(function (d) {
      // Store a reference to the circle DOM element in the node data
      d.domElement = this;
    });

  // Add text to the midpoint of the link
  links
    .append("text")
    .attr("x", (d) => (d.source.y + d.target.y) / 2)
    .attr("y", (d) => (d.source.x + d.target.x) / 2)
    .text((d) => d.target.data.Action || "");

  // Nodes
  const nodes = g
    .selectAll(".node")
    .data(root.descendants())
    .enter()
    .append("g")
    .attr("class", "node")
    .attr("transform", (d) => `translate(${d.y},${d.x})`);

  nodes
    .append("circle")
    .attr("r", 12)
    .each(function (d) {
      // Store a reference to the circle DOM element in the node data
      d.domElement = this;
    })
    .on("click", (_, d) => {
      if (selectedNode) {
        selectedNode.classed("selected", false);
      }

      selectedNode = d3.select(d.domElement);
      selectedNode.classed("selected", true);

      const { Children, ...nodeDetails } = d.data;
      document.getElementById("node-details").textContent = JSON.stringify(
        nodeDetails,
        null,
        2
      );
    })
    .on("mouseover", (_, d) => {
      const ancestors = d.ancestors();

      for (const node of ancestors) {
        d3.select(node.domElement).classed("hovered", true);

        if (node.linkToParent)
          d3.select(node.linkToParent.domElement).classed("hovered", true);
      }
    })
    .on("mouseout", (_, d) => {
      const ancestors = d.ancestors();

      for (const node of ancestors) {
        d3.select(node.domElement).classed("hovered", false);

        if (node.linkToParent)
          d3.select(node.linkToParent.domElement).classed("hovered", false);
      }
    });
}

loadAndRenderTree();
