#!/bin/bash

set -euo pipefail

# === Configuration ===
MCTS_OPPONENTS=("Static" "Walking" "Bombing" "Bombing2")
SEEDS=$(seq 1 10)
REPEATS=$(seq 1 3)
TIMEOUT=0:1:0
EXE="./Bomberman.Desktop/bin/Release/net8.0/Bomberman.Desktop.exe" 
REPORT_DIR="analysis/agent-breakdown/mcts-vs-mcts"

mkdir -p "$REPORT_DIR"

# === Run Experiments ===
for rep in $REPEATS; do
  for seed in $SEEDS; do
    for opp in "${MCTS_OPPONENTS[@]}"; do
      for mcts_opp in "${MCTS_OPPONENTS[@]}"; do  
        report_file="${REPORT_DIR}/mcts-vs-mcts-${opp}-${mcts_opp}-map${seed}.json"
        echo "Running: P2 change=$opp, P1 change=$mcts_opp, Seed=$seed, Rep=$rep"
        
        mcts_opts=$([ "$mcts_opp" == "" ] && echo "{}" || echo "{\"OpponentType\": \"$mcts_opp\"}")
        opp_opts=$([ "$opp" == "" ] && echo "{}" || echo "{\"OpponentType\": \"$opp\"}")

        "$EXE" \
          --seed "$seed" \
          --playerOne mcts "$mcts_opts" \
          --playerTwo mcts "$opp_opts" \
          --timeout "$TIMEOUT" \
          --report "$report_file" || echo "Error on P2 change=$opp, P1 change=$mcts_opp, Seed=$seed, Rep=$rep"

      done
    done
  done
done

echo "All experiments completed."
