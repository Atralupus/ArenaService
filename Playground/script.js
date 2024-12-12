const apiUrl = 'http://localhost:5000/api'; // Adjust the port if necessary
let participantId = 1; // Default participant ID

function setParticipantId() {
    const input = document.getElementById('participantId').value;
    participantId = parseInt(input, 10);
    displayOutput(`Participant ID set to ${participantId}`);
}

async function getCurrentSeason() {
    try {
        const response = await fetch(`${apiUrl}/seasons/current`);
        if (!response.ok) throw new Error('Failed to fetch current season');
        const season = await response.json();
        displayOutput(`Current Season: ID ${season.id}, Start: ${new Date(season.startTime).toLocaleString()}, End: ${new Date(season.endTime).toLocaleString()}`);
    } catch (error) {
        displayOutput(`Error: ${error.message}`);
    }
}

async function getLeaderboard() {
    try {
        const response = await fetch(`${apiUrl}/ranking/list?pageNumber=1&pageSize=10`);
        if (!response.ok) throw new Error('Failed to fetch leaderboard');
        const leaderboard = await response.json();
        let output = 'Leaderboard:\n';
        leaderboard.rankings.forEach((entry, index) => {
            output += `${index + 1}. ${entry.nickname} - Score: ${entry.totalScore}\n`;
        });
        displayOutput(output);
    } catch (error) {
        displayOutput(`Error: ${error.message}`);
    }
}

async function generateOpponents() {
    try {
        const response = await fetch(`${apiUrl}/challenge/generate-opponents?participantId=${participantId}`, {
            method: 'POST'
        });
        if (!response.ok) throw new Error('Failed to generate opponents');
        const result = await response.json();
        let output = 'Generated Opponents:\n';
        result.forEach(opponent => {
            output += `ID: ${opponent.opponentId}, Nickname: ${opponent.nickname}\n`;
        });
        displayOutput(output);
    } catch (error) {
        displayOutput(`Error: ${error.message}`);
    }
}

async function getAvailableOpponents() {
    try {
        const response = await fetch(`${apiUrl}/challenge/available-opponents?participantId=${participantId}`);
        if (!response.ok) throw new Error('Failed to fetch available opponents');
        const opponents = await response.json();
        let output = 'Available Opponents:\n';
        const opponentSelect = document.getElementById('opponentSelect');
        opponentSelect.innerHTML = ''; // Clear existing options
        opponents.forEach(opponent => {
            output += `ID: ${opponent.id}, Nickname: ${opponent.nickname}\n`;
            const option = document.createElement('option');
            option.value = opponent.id;
            option.text = opponent.nickname;
            opponentSelect.add(option);
        });
        displayOutput(output);
    } catch (error) {
        displayOutput(`Error: ${error.message}`);
    }
}

async function startBattle() {
    const opponentSelect = document.getElementById('opponentSelect');
    const opponentId = opponentSelect.value;
    if (!opponentId) {
        displayOutput('Please select an opponent.');
        return;
    }

    try {
        const response = await fetch(`${apiUrl}/battle/start?participantId=${participantId}&opponentId=${opponentId}`, {
            method: 'POST'
        });
        if (!response.ok) throw new Error('Failed to start battle');
        const result = await response.json();
        displayOutput(`Battle Result: ${result.result}, Score Change: ${result.scoreChange}`);
    } catch (error) {
        displayOutput(`Error: ${error.message}`);
    }
}

function displayOutput(message) {
    const outputDiv = document.getElementById('output');
    outputDiv.innerHTML = `<pre>${message}</pre>`;
} 