const absoluteCanvas = document.getElementById("absolute");
const relativeCanvas = document.getElementById("relative");
const foregroundTextures = [];
const textures = {};

const tileSize = 32;
const relativeMapSize = 31;

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
      X: 15 * tileSize + dx,
      Y: 15 * tileSize + dy
    },
    Alive: true
  })) : [];
  return states.concat(...node.Children.map(n => gatherRelative(n)))
}

const squareEmptyTileMap = [...Array(relativeMapSize)].map(_ => Array(relativeMapSize).fill(-1))
squareEmptyTileMap[Math.floor(relativeMapSize / 2)][Math.floor(relativeMapSize / 2)] = 2

function processNode(node) {
  const absolutePlayers = gatherAbsolute(node);
  drawTileMap(absoluteCanvas, node.State.TileMap, absolutePlayers);

  const relativePlayers = gatherRelative(node);
  drawTileMap(relativeCanvas, squareEmptyTileMap, relativePlayers);

  const distance = relativePlayers
    .map(({ Position: { X, Y } }) => (Math.abs(X / tileSize - Math.floor(relativeMapSize / 2)) + Math.abs(Y / tileSize - Math.floor(relativeMapSize / 2))))
    .reduce((acc, cur) => acc + cur, 0)
    / relativePlayers.length;
  document.getElementById("distance").textContent = distance;
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
