// Test script to validate team storage functionality
import {
  storeTeamNames,
  getStoredTeamNames,
  clearStoredTeamNames,
  getTeamNameWithFallback,
  STORAGE_KEYS,
} from '@/lib/teamStorage';

// Simulate browser environment for testing
const mockLocalStorage = {
  store: new Map<string, string>(),
  setItem: function (key: string, value: string) {
    this.store.set(key, value);
  },
  getItem: function (key: string) {
    return this.store.get(key) || null;
  },
  removeItem: function (key: string) {
    this.store.delete(key);
  },
};

// @ts-ignore
global.window = {};
// @ts-ignore
global.localStorage = mockLocalStorage;

// Test the functionality
console.log('🧪 Testing Team Storage Functionality...');

// Test storing team names
console.log('\n1. Testing storeTeamNames...');
storeTeamNames('Barcelona', 'Real Madrid');
console.log('✅ Stored team names');

// Test retrieving team names
console.log('\n2. Testing getStoredTeamNames...');
const stored = getStoredTeamNames();
console.log('✅ Retrieved teams:', stored);

// Test fallback functionality
console.log('\n3. Testing getTeamNameWithFallback...');
const homeTeam = getTeamNameWithFallback(
  undefined,
  STORAGE_KEYS.HOME_TEAM,
  'Default Home'
);
const awayTeam = getTeamNameWithFallback(
  'Custom Away',
  STORAGE_KEYS.AWAY_TEAM,
  'Default Away'
);
console.log('✅ Home team (from storage):', homeTeam);
console.log('✅ Away team (from parameter):', awayTeam);

// Test clearing
console.log('\n4. Testing clearStoredTeamNames...');
clearStoredTeamNames();
const afterClear = getStoredTeamNames();
console.log('✅ After clearing:', afterClear);

console.log('\n🎉 All tests completed successfully!');
