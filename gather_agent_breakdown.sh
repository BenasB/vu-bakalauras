#!/bin/bash

set -euo pipefail

# === Configuration ===
MCTS_OPPONENTS=("" "Static" "Walking" "Bombing" "Bombing2")
SEEDS=$(seq 1 10)
REPEATS=$(seq 1 1)
REAL_OPPONENTS=("bombing2")
TIMEOUT=0:1:0
EXE="./Bomberman.Desktop/bin/Release/net8.0/Bomberman.Desktop.exe" 
REPORT_DIR="analysis/agent-breakdown"

mkdir -p "$REPORT_DIR"

# === Run Experiments ===
for seed in $SEEDS; do
  for real_opp in "${REAL_OPPONENTS[@]}"; do
    for mcts_opp in "${MCTS_OPPONENTS[@]}"; do
      for rep in $REPEATS; do
        report_file="${REPORT_DIR}/mcts-vs-${real_opp}-${mcts_opp}-map${seed}.json"
        echo "Running: Real opponent=$real_opp, MCTS opponent=$mcts_opp, Seed=$seed, Rep=$rep"
        
        mcts_opts=$([ "$mcts_opp" == "" ] && echo "{}" || echo "{\"OpponentType\": \"$mcts_opp\"}")

        "$EXE" \
          --seed "$seed" \
          --playerOne mcts "$mcts_opts" \
          --playerTwo "$real_opp" \
          --timeout "$TIMEOUT" \
          --report "$report_file" || echo "Error on Seed $seed, Real opponent=$real_opp, MCTS opponent=$mcts_opp, Rep=$rep"

      done
    done
  done
done

echo "All experiments completed."
