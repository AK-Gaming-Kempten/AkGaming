const leagues = {
  "Iron": 0,
  "Bronze": 1,
  "Silver": 2,
  "Gold": 3,
  "Platinum": 4,
  "Emerald": 5,
  "Diamond": 6,
  "Master+": 7
};

const divisions = {
  "I": 0,
  "II": 1,
  "III": 2,
  "IV": 3
};

const rankToBaseLP = {};
Object.keys(leagues).forEach((league, index) => {
  if (league !== "Master+") {
    rankToBaseLP[league] = index * 400;
  } else {
    rankToBaseLP[league] = 2800;  // Master+ LP starts at 2800, adjust as necessary
  }
});

const diamond499 = 2499;

const playersDiv = document.getElementById('players');
for (let i = 1; i <= 7; i++) {
  const div = document.createElement('div');
  div.classList.add('player');
  div.id = `player-container-${i}`;
  div.innerHTML = `
    <label>Player ${i} Rank</label>
    <div class="flex-container">
      <img id="leagueIcon${i}" class="league-icon" src="images/RankIcons/Iron.webp" alt="League Icon" />
      <select id="league${i}" class="selectLeague" onchange="updateLeagueImage(${i}); calculateAverage();">
        ${Object.keys(leagues).map(league => `<option value="${league}">${league}</option>`).join('')}
      </select>
      <select id="division${i}" class="selectDivision" onchange="calculateAverage();">
        <option value="IV">IV</option>
        <option value="III">III</option>
        <option value="II">II</option>
        <option value="I">I</option>
      </select>
      <input class="inputLP" oninput="calculateAverage()" type="number" id="lp${i}" min="0" value="0" />
      <label>LP</label>
    </div>
    <div class="comparison-text" id="comparison${i}"></div>
  `;
  playersDiv.appendChild(div);
}

window.onload = function() {
  calculateAverage(); // or whatever function you want
};

function calculateBaseLP(league, division) {
  if (league === "Master+") return 2800;
  return (leagues[league] * 400) + (3 - divisions[division]) * 100;
}

function updateLeagueImage(id) {
  const league = document.getElementById(`league${id}`).value;
  const icon = document.getElementById(`leagueIcon${id}`);
  icon.src = `images/RankIcons/${league}.webp`;

  const divisionDropdown = document.getElementById(`division${id}`);
  divisionDropdown.disabled = (league === "Master+");
}

function updateComparisonText(container, newText, colorClass) {
  const oldSpan = container.querySelector('span');
  const newSpan = document.createElement('span');
  newSpan.innerHTML = newText;

  // Apply initial styles
  newSpan.classList.add('fade-out');
  container.className = `comparison-text ${colorClass}`;
  container.appendChild(newSpan);

  // Trigger fade-in of new and fade-out of old
  requestAnimationFrame(() => {
    newSpan.classList.remove('fade-out');
    if (oldSpan) oldSpan.classList.add('fade-out');
  });

  // Remove old after fade
  setTimeout(() => {
    if (oldSpan && oldSpan.parentNode === container) {
      container.removeChild(oldSpan);
    
    }
  }, 400);
}

function getRankFromLP(lp) {
  let closestRank = "Master+";
  for (let i = Object.keys(leagues).length - 1; i >= 0; i--) {
    if (lp >= rankToBaseLP[Object.keys(leagues)[i]]) {
      closestRank = Object.keys(leagues)[i];
      break;
    }
  }
  return closestRank;
}

function calculateAverage() {
  const lpList = [];

  for (let i = 1; i <= 7; i++) {
    const league = document.getElementById(`league${i}`).value;
    const division = document.getElementById(`division${i}`).value;
    const lpInput = parseInt(document.getElementById(`lp${i}`).value);
    const container = document.getElementById(`player-container-${i}`);
    const comparison = document.getElementById(`comparison${i}`);
    comparison.innerHTML = "";
    container.classList.remove('top5', 'faded');

    if (league && (league === "Master+" || division) && !isNaN(lpInput) && lpInput >= 0) {
      const baseLP = calculateBaseLP(league, division);
      const totalLP = baseLP + lpInput;
      lpList.push({ id: i, name: `Player ${i}`, lp: totalLP });
    }
  }

  if (lpList.length < 5) {
    document.getElementById('result').innerHTML = "<span class='disqualified'>Please enter valid rank and LP for at least 5 players.</span>";
    return;
  }

  lpList.sort((a, b) => b.lp - a.lp);
  const top5 = lpList.slice(0, 5);
  const avg = Math.round(top5.reduce((sum, p) => sum + p.lp, 0) / 5);
  const avgDiff = avg - diamond499;
  const avgRank = getRankFromLP(avg);
  const qualifies = avg < diamond499;
  const top5Ids = top5.map(p => p.id);

  for (let i = 1; i <= 7; i++) {
    const container = document.getElementById(`player-container-${i}`);
    const comparison = document.getElementById(`comparison${i}`);
    const playerData = lpList.find(p => p.id === i);

    if (!playerData) {
      container.classList.add('faded');
      continue;
    }

    const diff = playerData.lp - diamond499;
    const isTop5 = top5Ids.includes(i);
    const colorClass = diff >= 0
      ? (isTop5 ? 'red' : 'red-faded')
      : (isTop5 ? 'green' : 'green-faded');

    const comparisonText = `${playerData.lp} LP (${diff >= 0 ? '+' : ''}${diff} vs D4 99LP)`;
    updateComparisonText(comparison, comparisonText, colorClass);

    if (!isTop5) {
      container.classList.add('faded');
    } else {
      container.classList.add('top5');
    }
  }

  const avgRankIcon = `images/Season_2023_-_${avgRank}.webp`;

  document.getElementById('result').innerHTML = `
    <div><img src="${avgRankIcon}" class="avg-rank-icon" alt="Avg Rank Icon"> Team Avg: ${avg} LP (${avgDiff >= 0 ? '+' : ''}${avgDiff} vs Diamond 4 99LP)</div>
    → <strong>${avgRank}</strong>
    <br>
    ${qualifies
      ? "<span class='qualifies'>✅ Team qualifies for the tournament</span>"
      : "<span class='disqualified'>❌ Team does NOT qualify</span>"
    }
  `;
}

