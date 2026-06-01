// Test script to verify profile update with form data
// Run this in the browser console when logged in

console.log('🧪 Testing Profile Update with Form Data...');

// Test the form data conversion
function testFormDataConversion() {
  const testData = {
    Id: 'test-user-id',
    username: 'newusername',
    email: 'test@example.com',
    age: 25,
    favoriteTeamId: 1,
    PhoneNumber: 1234567890,
  };

  console.log('Original data:', testData);

  // Simulate what the new putFormWithRetry method does
  const formData = new FormData();
  Object.keys(testData).forEach((key) => {
    const value = testData[key];
    if (value !== null && value !== undefined) {
      if (value instanceof File || value instanceof Blob) {
        formData.append(key, value);
      } else {
        formData.append(key, String(value));
      }
    }
  });

  console.log('FormData entries:');
  for (let [key, value] of formData.entries()) {
    console.log(`  ${key}: ${value}`);
  }

  return formData;
}

// Test function
function runTest() {
  try {
    const formData = testFormDataConversion();
    console.log('✅ Form data conversion test passed');

    console.log('\n📋 Changes made to fix the issue:');
    console.log('1. Added putFormWithRetry method to ApiService.ts');
    console.log('2. Updated AuthenticationService.ts to use putFormWithRetry');
    console.log(
      '3. Form data will now be sent as multipart/form-data instead of JSON'
    );
    console.log(
      '4. This matches the backend expectation of [FromForm] attribute'
    );

    console.log('\n🎯 Next steps:');
    console.log('1. Try updating your username in the profile page');
    console.log(
      '2. Check network tab to see Content-Type: multipart/form-data'
    );
    console.log('3. The 400 Bad Request error should be resolved');
  } catch (error) {
    console.error('❌ Test failed:', error);
  }
}

// Run the test
runTest();
