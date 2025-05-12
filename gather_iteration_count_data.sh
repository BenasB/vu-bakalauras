#!/bin/bash

set -euo pipefail

# === Configuration ===
SLOW_DOWN_TICKS=$(seq 0 40000 320000)
SEEDS=$(seq 1 1)
REPEATS=$(seq 1 1)
OPPONENTS=("static" "walking" "bombing")
TIMEOUT=0:1:0
EXE="./Bomberman.Desktop/bin/Release/net8.0/Bomberman.Desktop.exe" 
REPORT_DIR="analysis/iteration-count"

mkdir -p "$REPORT_DIR"

# === Run Experiments ===
for s in $SLOW_DOWN_TICKS; do
  for opp in "${OPPONENTS[@]}"; do
    for seed in $SEEDS; do
      for rep in $REPEATS; do
        report_file="${REPORT_DIR}/mcts-vs-${opp}-map${seed}-s${s}.json"
        echo "Running: Opponent=$opp, Seed=$seed, SlowDownTicks=$s, Rep=$rep"

        "$EXE" \
          --seed "$seed" \
          --playerOne mcts "{\"SlowDownTicks\": $s}" \
          --playerTwo "$opp" \
          --timeout "$TIMEOUT" \
          --report "$report_file" || echo "Error on Seed $seed, Opponent $opp, c=$c, Rep $rep"

      done
    done
  done
done

echo "All experiments completed."
