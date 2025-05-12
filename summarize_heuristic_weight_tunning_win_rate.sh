#!/bin/bash

# This script processes JSON files containing MCTS agent results and generates
# a CSV file with win rates grouped by c-value and opponent type

if [ $# -ne 1 ]; then
    echo "Usage: $0 <directory-containing-json-files>"
    exit 1
fi

directory="$1"

# Initialize associative arrays to store counts
declare -A wins
declare -A totals

# Process each JSON file in the directory
for file in "$directory"/*.json; do
    # Skip directories
    [ -d "$file" ] && continue
    
    # Extract c-value from filename (format: mcts-vs-opponent-mapX-cX.XX.json)
    filename=$(basename "$file")
    if [[ "$filename" =~ c([0-9.]+) ]]; then
        c_value="${BASH_REMATCH[1]}"
    else
        echo "Warning: Could not extract c-value from $filename" >&2
        continue
    fi
    
    # Extract opponent type from filename
    if [[ "$filename" =~ mcts-vs-([a-zA-Z]+)- ]]; then
        opponent="${BASH_REMATCH[1]}"
    else
        echo "Warning: Could not extract opponent from $filename" >&2
        continue
    fi
    
    # Count total games and wins
    game_count=$(jq 'length' "$file" 2>/dev/null)
    [ -z "$game_count" ] && continue
    
    win_count=$(jq '[.[] | select(.Terminated == true and .MctsAgent.Alive == true)] | length' "$file" 2>/dev/null)
    [ -z "$win_count" ] && continue
    
    # Update totals
    ((wins["$c_value,$opponent"]+=win_count))
    ((totals["$c_value,$opponent"]+=game_count))
done

# Get unique c-values (sorted numerically) and opponent types (sorted alphabetically)
readarray -t c_values < <(printf '%s\n' "${!totals[@]}" | cut -d, -f1 | sort -n | uniq)
readarray -t opponents < <(printf '%s\n' "${!totals[@]}" | cut -d, -f2 | sort | uniq)

# Output CSV header
echo -n "c_value"
for opponent in "${opponents[@]}"; do
    echo -n ",${opponent}_winrate"
done
echo  # newline

# Output data rows
for c in "${c_values[@]}"; do
    echo -n "$c"
    for opponent in "${opponents[@]}"; do
        key="$c,$opponent"
        if [[ -n "${wins[$key]}" && -n "${totals[$key]}" && ${totals[$key]} -ne 0 ]]; then
            win_rate=$(awk -v wins="${wins[$key]}" -v total="${totals[$key]}" 'BEGIN { printf "%.4f", wins/total }')
            echo -n ",$win_rate"
        else
            echo -n ",0.0000"
        fi
    done
    echo  # newline
done