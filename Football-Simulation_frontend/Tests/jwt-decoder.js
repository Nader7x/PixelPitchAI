// JWT Token Decoder Utility for debugging
// Paste this into browser console to decode your JWT token

const token =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW5AZm9vdGV4LmNvbSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6ImFkbWluQGZvb3RleC5jb20iLCJqdGkiOiIxNWNlOWFlMi1mZGEwLTRjZmMtODY4Ny05MzkyZWVmMDBmNTkiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImUyNjliZjQzLTU2YTktNDE0ZS1iY2ZjLTQxNDRkOTg0ZWRhYSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFkbWluIiwiZXhwIjoxNzUwODYxOTgzLCJpc3MiOiJodHRwczovL2Zvb3RleC5jb20iLCJhdWQiOiJodHRwczovL2Zvb3RleC5jb20ifQ.VLvfcstr5_SvKp_OlpZ2vBekL1UTdm6bqt_H1ADkykQ';

// Decode the JWT token
function decodeJWT(token) {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new Error('Invalid JWT token format');
    }

    const payload = parts[1];
    const decoded = JSON.parse(
      atob(payload.replace(/_/g, '/').replace(/-/g, '+'))
    );

    return decoded;
  } catch (error) {
    console.error('Error decoding JWT:', error);
    return null;
  }
}

const decodedToken = decodeJWT(token);

console.log('🔍 Decoded JWT Token:');
console.log(JSON.stringify(decodedToken, null, 2));

console.log('\n📋 Extracted Claims:');
console.log(
  'Name:',
  decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
);
console.log(
  'Email:',
  decodedToken[
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
  ]
);
console.log(
  'User ID (nameidentifier):',
  decodedToken[
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
  ]
);
console.log(
  'Role:',
  decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
);
console.log('JTI:', decodedToken.jti);
console.log('Expires:', new Date(decodedToken.exp * 1000));

console.log('\n✅ Key Information:');
console.log(
  'User ID for API calls:',
  decodedToken[
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
  ]
);
console.log(
  'Token expires:',
  new Date(decodedToken.exp * 1000).toLocaleString()
);
console.log('Is token expired?', Date.now() > decodedToken.exp * 1000);

// Expected API URL
const userId =
  decodedToken[
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
  ];
console.log('\n🌐 Expected API URL:');
console.log(`https://localhost:7082/api/matches/livematch/${userId}`);

export { decodeJWT, decodedToken };
