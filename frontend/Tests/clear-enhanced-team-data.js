// Clear any enhanced team data from localStorage
if (typeof window !== 'undefined') {
  // Clear enhanced team data keys that were added during the session
  localStorage.removeItem('homeTeamData');
  localStorage.removeItem('awayTeamData');
  localStorage.removeItem('HOME_TEAM_DATA');
  localStorage.removeItem('AWAY_TEAM_DATA');

  // Also clear any other potential leftover keys
  const keys = Object.keys(localStorage);
  keys.forEach((key) => {
    if (key.includes('TeamData') || key.includes('teamData')) {
      localStorage.removeItem(key);
      console.log(`Removed leftover key: ${key}`);
    }
  });

  console.log('✅ Cleared all enhanced team data from localStorage');
  console.log('📦 Current localStorage keys:', Object.keys(localStorage));
} else {
  console.log('❌ Not in browser environment');
}
