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
    if [[ "$filename" =~ mcts-vs-([a-zA-Z0-9]+)-([a-zA-Z0-9]*)- ]]; then
        real_opponent="${BASH_REMATCH[1]}"
        opponent_type="${BASH_REMATCH[2]:-no_change}"
    else
        echo "Warning: Could not extract opponent from $filename" >&2
        continue
    fi
    
    game_count=$(jq 'length' "$file" 2>/dev/null)
    [ -z "$game_count" ] && continue
    
    win_count=$(jq '[.[] | select(.Terminated == true and .MctsAgent.Alive == true)] | length' "$file" 2>/dev/null)
    [ -z "$win_count" ] && continue
    
    # Matches be draw as well, needs fixing
    lose_count=$(jq '[.[] | select(.Terminated == true and .MctsAgent.Alive == false)] | length' "$file" 2>/dev/null)
    [ -z "$lose_count" ] && continue
    
    draw_count=$(jq 'map([to_entries[] | select(.key | contains("Agent")) | .value.Alive] | if length > 0 then all(. == false) else false end) | [.[] | select(.)] | length' "$file" 2>/dev/null)
    [ -z "$draw_count" ] && continue
    
    timeout_count=$(jq '[.[] | select(.Terminated == false)] | length' "$file" 2>/dev/null)
    [ -z "$timeout_count" ] && continue
    
    # Update totals
    ((wins["$real_opponent,$opponent_type"]+=win_count))
    ((loses["$real_opponent,$opponent_type"]+=lose_count))
    ((draws["$real_opponent,$opponent_type"]+=draw_count))
    ((timeouts["$real_opponent,$opponent_type"]+=timeout_count))
    ((totals["$real_opponent,$opponent_type"]+=game_count))
done

readarray -t real_opponents < <(printf '%s\n' "${!totals[@]}" | cut -d, -f1 | sort -n | uniq)
readarray -t opponent_types < <(printf '%s\n' "${!totals[@]}" | cut -d, -f2 | sort | uniq)

# Output CSV header
echo -n "real_opponent"
for opponent_type in "${opponent_types[@]}"; do
    echo -n ",${opponent_type}_win"
    echo -n ",${opponent_type}_loss"
    echo -n ",${opponent_type}_draw"
    echo -n ",${opponent_type}_timeout"
done
echo  # newline

# Output data rows
for real_opp in "${real_opponents[@]}"; do
    echo -n "$real_opp"
    for opponent_type in "${opponent_types[@]}"; do
        key="$real_opp,$opponent_type"
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