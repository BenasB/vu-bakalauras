#!/bin/bash

if [ $# -ne 1 ]; then
    echo "Usage: $0 <directory-containing-json-files>"
    exit 1
fi

directory="$1"

# Initialize associative arrays
declare -A distance_sums
declare -A bomb_sums
declare -A game_counts

# Process each JSON file
for file in "$directory"/*.json; do
    # Skip directories and non-files
    [ ! -f "$file" ] && continue

    # Extract s-value from filename
    filename=$(basename "$file")
    if [[ "$filename" =~ s([0-9.]+) ]]; then
        s_value="${BASH_REMATCH[1]}"
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
    
    # Sum all BombsPlaced values
    bomb_sum=$(jq '[.[] | .MctsAgent.BombsPlaced] | add' "$file" 2>/dev/null)
    [ -z "$bomb_sum" ] && continue

    # Update totals
    key="$s_value,$opponent"
    distance_sums["$key"]=$(awk -v sum="${distance_sums[$key]:-0}" -v new="$distance_sum" 'BEGIN { print sum + new }')
    bomb_sums["$key"]=$(awk -v sum="${bomb_sums[$key]:-0}" -v new="$bomb_sum" 'BEGIN { print sum + new }')
    game_counts["$key"]=$(( ${game_counts[$key]:-0} + game_count ))
done

# Get unique sorted values
readarray -t s_values < <(printf '%s\n' "${!game_counts[@]}" | cut -d, -f1 | sort -n | uniq)
readarray -t opponents < <(printf '%s\n' "${!game_counts[@]}" | cut -d, -f2 | sort | uniq)

# Output CSV header
echo -n "s_value"
for opponent in "${opponents[@]}"; do
    echo -n ",${opponent}_avg_distance,${opponent}_avg_bombs_placed"
done
echo

# Output data rows
for c in "${s_values[@]}"; do
    echo -n "$c"
    for opponent in "${opponents[@]}"; do
        key="$c,$opponent"
        if [[ -n "${distance_sums[$key]}" && -n "${game_counts[$key]}" && ${game_counts[$key]} -ne 0 ]]; then
            avg_distance=$(awk -v sum="${distance_sums[$key]}" -v count="${game_counts[$key]}" 'BEGIN { printf "%.2f", sum/count }')
            echo -n ",$avg_distance"
        else
            echo -n ",NA"
        fi
        if [[ -n "${bomb_sums[$key]}" && -n "${game_counts[$key]}" && ${game_counts[$key]} -ne 0 ]]; then
            avg_bomb_placed=$(awk -v sum="${bomb_sums[$key]}" -v count="${game_counts[$key]}" 'BEGIN { printf "%.2f", sum/count }')
            echo -n ",$avg_bomb_placed"
        else
            echo -n ",NA"
        fi
    done
    echo
done