#!/bin/bash

set -euo pipefail

# === Configuration ===
SEEDS=$(seq 1 10)
REPEATS=$(seq 1 1)
PLAYER_ONE=("bombing" "mcts")
TIMEOUT=0:1:0
EXE="./Bomberman.Desktop/bin/Release/net8.0/Bomberman.Desktop.exe" 
REPORT_DIR="analysis/navigate-and-destroy"

mkdir -p "$REPORT_DIR"

# === Run Experiments ===
for p1 in "${PLAYER_ONE[@]}"; do
  for seed in $SEEDS; do
    for rep in $REPEATS; do
      report_file="${REPORT_DIR}/${p1}-vs-static-map${seed}.json"
      echo "Running: Player one=$p1, Seed=$seed, Rep=$rep"

      "$EXE" \
        --seed "$seed" \
        --playerOne "$p1" \
        --playerTwo "static" \
        --timeout "$TIMEOUT" \
        --report "$report_file" || echo "Error on Seed $seed, Player one $p1, Rep $rep"

    done
  done
done

echo "All experiments completed."
