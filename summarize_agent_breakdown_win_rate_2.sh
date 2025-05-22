#!/bin/bash

if [ $# -ne 1 ]; then
    echo "Usage: $0 <directory-containing-json-files>"
    exit 1
fi

directory="$1"

# Initialize associative arrays to store counts
declare -A wins
declare -A loses
declare -A draws
declare -A timeouts
declare -A totals

# Process each JSON file in the directory
for file in "$directory"/*.json; do
    # Skip directories
    [ -d "$file" ] && continue
    
    # Extract opponent types from filename
    filename=$(basename "$file")
    if [[ "$filename" =~ mcts-vs-mcts-([a-zA-Z0-9]+)-([a-zA-Z0-9]*)- ]]; then
        p2_change="${BASH_REMATCH[1]}"
        p1_change="${BASH_REMATCH[2]}"
    else
        echo "Warning: Could not extract opponent from $filename" >&2
        continue
    fi
    
    game_count=$(jq 'length' "$file" 2>/dev/null)
    [ -z "$game_count" ] && continue
    
    win_count=$(jq '[.[] | select(.Terminated == true and .Agents[0].Alive == true and .Agents[1].Alive == false)] | length' "$file" 2>/dev/null)
    [ -z "$win_count" ] && continue
    
    lose_count=$(jq '[.[] | select(.Terminated == true and .Agents[0].Alive == false and .Agents[1].Alive == true)] | length' "$file" 2>/dev/null)
    [ -z "$lose_count" ] && continue
    
    draw_count=$(jq '[.[] | select(.Terminated == true and .Agents[0].Alive == false and .Agents[1].Alive == false)] | length' "$file" 2>/dev/null)
    [ -z "$draw_count" ] && continue
    
    timeout_count=$(jq '[.[] | select(.Terminated == false)] | length' "$file" 2>/dev/null)
    [ -z "$timeout_count" ] && continue
    
    # Update totals
    ((wins["$p2_change,$p1_change"]+=win_count))
    ((loses["$p2_change,$p1_change"]+=lose_count))
    ((draws["$p2_change,$p1_change"]+=draw_count))
    ((timeouts["$p2_change,$p1_change"]+=timeout_count))
    ((totals["$p2_change,$p1_change"]+=game_count))
done

readarray -t p2_changes < <(printf '%s\n' "${!totals[@]}" | cut -d, -f1 | sort -n | uniq)
readarray -t p1_changes < <(printf '%s\n' "${!totals[@]}" | cut -d, -f2 | sort | uniq)

# Output CSV header
echo -n "p2_change"
for p1_change in "${p1_changes[@]}"; do
    echo -n ",${p1_change}_win"
    echo -n ",${p1_change}_loss"
    echo -n ",${p1_change}_draw"
    echo -n ",${p1_change}_timeout"
done
echo  # newline

# Output data rows
for p2_change in "${p2_changes[@]}"; do
    echo -n "$p2_change"
    for p1_change in "${p1_changes[@]}"; do
        key="$p2_change,$p1_change"
        if [[ -n "${wins[$key]}" && -n "${totals[$key]}" && ${totals[$key]} -ne 0 ]]; then
            win_rate=$(awk -v wins="${wins[$key]}" -v total="${totals[$key]}" 'BEGIN { printf "%.4f", wins/total }')
            echo -n ",$win_rate"
        else
            echo -n ",0.0000"
        fi
        if [[ -n "${loses[$key]}" && -n "${totals[$key]}" && ${totals[$key]} -ne 0 ]]; then
            loss_rate=$(awk -v loses="${loses[$key]}" -v total="${totals[$key]}" 'BEGIN { printf "%.4f", loses/total }')
            echo -n ",$loss_rate"
        else
            echo -n ",0.0000"
        fi
        if [[ -n "${draws[$key]}" && -n "${totals[$key]}" && ${totals[$key]} -ne 0 ]]; then
            draw_rate=$(awk -v draws="${draws[$key]}" -v total="${totals[$key]}" 'BEGIN { printf "%.4f", draws/total }')
            echo -n ",$draw_rate"
        else
            echo -n ",0.0000"
        fi
        if [[ -n "${timeouts[$key]}" && -n "${totals[$key]}" && ${totals[$key]} -ne 0 ]]; then
            timeout_rate=$(awk -v timeouts="${timeouts[$key]}" -v total="${totals[$key]}" 'BEGIN { printf "%.4f", timeouts/total }')
            echo -n ",$timeout_rate"
        else
            echo -n ",0.0000"
        fi
    done
    echo  # newline
done