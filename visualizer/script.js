const canvas = document.getElementById("tileMap");
const ctx = canvas.getContext("2d");

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

async function loadAndRenderTree(treeData) {
  g.selectAll("*").remove();

  const root = d3.hierarchy(treeData, (d) => d.Children);
  root.sort((a, b) => a.data.Action.localeCompare(b.data.Action));
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

  const maxVisits = d3.max(root.descendants(), (d) => d.data.Visits);
  const minVisits = d3.min(root.descendants(), (d) => d.data.Visits);
  const circleSizeScale = d3
    .scaleLinear()
    .domain([minVisits, maxVisits])
    .range([10, 20]);

  nodes
    .append("circle")
    .attr("r", (d) => circleSizeScale(d.data.Visits))
    .classed("terminated", (d) => d.data.State.Terminated)
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

      const { Children, State, SimulationEndState, ...nodeDetails } = d.data;
      const { TileMap, ...stateDetails } = State;

      drawTileMap(TileMap, State.Players);

      document.getElementById("node-details").textContent = JSON.stringify(
        { ...nodeDetails, State: { ...stateDetails } },
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

const foregroundTextures = [];
const textures = {};

async function preloadTextures() {
  const textureFiles = [
    { name: "bomb", url: "textures/bomb.png" },
    { name: "box", url: "textures/box.png" },
    { name: "explosion", url: "textures/explosion.png" },
    { name: "floor", url: "textures/floor.png" },
    { name: "player", url: "textures/player.png" },
    { name: "wall", url: "textures/wall.png" },
  ];

  await Promise.all(
    textureFiles.map((file) => {
      return new Promise((resolve) => {
        const img = new Image();
        img.src = file.url;
        img.onload = () => {
          textures[file.name] = img;
          resolve();
        };
      });
    })
  );

  foregroundTextures.push(textures["bomb"]);
  foregroundTextures.push(textures["box"]);
  foregroundTextures.push(textures["explosion"]);
  foregroundTextures.push(textures["wall"]);
}

function drawTileMap(TileMap, Players) {
  const tileSize = 32;
  const width = TileMap[0].length;
  const height = TileMap.length;
  canvas.width = width * tileSize;
  canvas.height = height * tileSize;
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  for (let row = 0; row < height; row++) {
    for (let column = 0; column < width; column++) {
      ctx.drawImage(
        textures["floor"],
        column * tileSize,
        row * tileSize,
        tileSize,
        tileSize
      );

      const textureIndex = TileMap[row][column]
      if (textureIndex == -1)
        continue;

      ctx.drawImage(
        foregroundTextures[textureIndex],
        column * tileSize,
        row * tileSize,
        tileSize,
        tileSize
      );
    }
  }

  Players.forEach(player => {
    if (!player.Alive)
      return;

    ctx.drawImage(
      textures["player"],
      player.Position.X,
      player.Position.Y,
      tileSize,
      tileSize
    );
  });
}

document.getElementById("json-upload").addEventListener("change", (event) => {
  const file = event.target.files[0];

  if (file) {
    const reader = new FileReader();

    reader.onload = async (e) => {
      try {
        const json = JSON.parse(e.target.result);
        await loadAndRenderTree(json);
      } catch (error) {
        alert("Error parsing JSON file. Please upload a valid JSON file.");
        console.error(error);
      }
    };

    reader.readAsText(file);
  }
});

preloadTextures();
