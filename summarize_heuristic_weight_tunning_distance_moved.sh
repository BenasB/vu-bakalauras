#!/bin/bash

# This script processes JSON files and generates a CSV of average DistanceMoved
# by the MCTS agent, grouped by c-value and opponent type

if [ $# -ne 1 ]; then
    echo "Usage: $0 <directory-containing-json-files>"
    exit 1
fi

directory="$1"

# Initialize associative arrays
declare -A distance_sums
declare -A game_counts

# Process each JSON file
for file in "$directory"/*.json; do
    # Skip directories and non-files
    [ ! -f "$file" ] && continue

    # Extract c-value from filename
    filename=$(basename "$file")
    if [[ "$filename" =~ c([0-9.]+) ]]; then
        c_value="${BASH_REMATCH[1]}"
    else
        echo "Warning: Could not extract c-value from $filename" >&2
        continue
    fi

    # Extract opponent type
    if [[ "$filename" =~ mcts-vs-([a-zA-Z]+)- ]]; then
        opponent="${BASH_REMATCH[1]}"
    else
        echo "Warning: Could not extract opponent from $filename" >&2
        continue
    fi

    # Process JSON data
    game_count=$(jq 'length' "$file" 2>/dev/null)
    [ -z "$game_count" ] && continue

    # Sum all DistanceMoved values
    distance_sum=$(jq '[.[] | .MctsAgent.DistanceMoved] | add' "$file" 2>/dev/null)
    [ -z "$distance_sum" ] && continue

    # Update totals
    key="$c_value,$opponent"
    distance_sums["$key"]=$(awk -v sum="${distance_sums[$key]:-0}" -v new="$distance_sum" 'BEGIN { print sum + new }')
    game_counts["$key"]=$(( ${game_counts[$key]:-0} + game_count ))
done

# Get unique sorted values
readarray -t c_values < <(printf '%s\n' "${!game_counts[@]}" | cut -d, -f1 | sort -n | uniq)
readarray -t opponents < <(printf '%s\n' "${!game_counts[@]}" | cut -d, -f2 | sort | uniq)

# Output CSV header
echo -n "c_value"
for opponent in "${opponents[@]}"; do
    echo -n ",${opponent}_avg_distance"
done
echo

# Output data rows
for c in "${c_values[@]}"; do
    echo -n "$c"
    for opponent in "${opponents[@]}"; do
        key="$c,$opponent"
        if [[ -n "${distance_sums[$key]}" && -n "${game_counts[$key]}" && ${game_counts[$key]} -ne 0 ]]; then
            avg_distance=$(awk -v sum="${distance_sums[$key]}" -v count="${game_counts[$key]}" 'BEGIN { printf "%.2f", sum/count }')
            echo -n ",$avg_distance"
        else
            echo -n ",NA"
        fi
    done
    echo
done