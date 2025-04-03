const absoluteCanvas = document.getElementById("absolute");
const relativeCanvas = document.getElementById("relative");
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

function drawTileMap(canvas, TileMap, Players) {
  const tileSize = 32;
  const width = TileMap[0].length;
  const height = TileMap.length;
  canvas.width = width * tileSize;
  canvas.height = height * tileSize;
  ctx = canvas.getContext("2d");
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

  ctx.globalAlpha = 0.1;

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

  ctx.globalAlpha = 1.0;
}

function gatherAbsolute(node) {
  const states = node.SimulationEndState ? [node.SimulationEndState.Players[0]] : [];
  return states.concat(...node.Children.map(n => gatherAbsolute(n)))
}

function gatherRelative(node) {
  const states = node.SimulationEndState ? [{
    dx: node.SimulationEndState.Players[0].Position.X - node.State.Players[0].Position.X,
    dy: node.SimulationEndState.Players[0].Position.Y - node.State.Players[0].Position.Y
  }].map(({ dx, dy }) => ({
    Position: {
      X: 15 * 32 + dx,
      Y: 15 * 32 + dy
    },
    Alive: true
  })) : [];
  return states.concat(...node.Children.map(n => gatherRelative(n)))
}

const squareEmptyTileMap = [...Array(31)].map(_ => Array(31).fill(-1))
squareEmptyTileMap[15][15] = 2

function processNode(node) {
  const absolutePlayers = gatherAbsolute(node);
  drawTileMap(absoluteCanvas, node.State.TileMap, absolutePlayers);

  const relativePlayers = gatherRelative(node);
  drawTileMap(relativeCanvas, squareEmptyTileMap, relativePlayers);
}

document.getElementById("json-upload").addEventListener("change", (event) => {
  const file = event.target.files[0];

  if (file) {
    const reader = new FileReader();

    reader.onload = async (e) => {
      try {
        const json = JSON.parse(e.target.result);
        processNode(json);
      } catch (error) {
        alert("Error parsing JSON file. Please upload a valid JSON file.");
        console.error(error);
      }
    };

    reader.readAsText(file);
  }
});

preloadTextures();
