#!/bin/bash

set -euo pipefail

# === Configuration ===
C_VALUES=(0.01 0.05 0.1 0.15 0.2 0.25 0.3 0.35 0.4 0.45 0.5 1.0 2.0)
SEEDS=$(seq 1 10)                    # Map seeds 1–10
REPEATS=$(seq 1 10)                  # 10 runs per map
OPPONENTS=("static" "walking" "bombing")
TIMEOUT=0:2:0                          # Seconds before killing the run
EXE="./Bomberman.Desktop/bin/Release/net8.0/Bomberman.Desktop.exe" 
REPORT_DIR="analysis/weight-tuning"          # Output directory

mkdir -p "$REPORT_DIR"

# === Run Experiments ===
for c in "${C_VALUES[@]}"; do
  for opp in "${OPPONENTS[@]}"; do
    for seed in $SEEDS; do
      for rep in $REPEATS; do
        report_file="${REPORT_DIR}/mcts-vs-${opp}-map${seed}-c${c}.json"
        echo "Running: Opponent=$opp, Seed=$seed, c=$c, Rep=$rep"

        "$EXE" \
          --seed "$seed" \
          --playerOne mcts "{\"SelectionHeuristicWeightCoefficient\": $c}" \
          --playerTwo "$opp" \
          --timeout "$TIMEOUT" \
          --report "$report_file" || echo "Error on Seed $seed, Opponent $opp, c=$c, Rep $rep"

      done
    done
  done
done

echo "All experiments completed."
