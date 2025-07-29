// components/SignIn.tsx

export default function SignIn() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <div className="mx-auto flex w-full max-w-sm overflow-hidden rounded-lg bg-white shadow-lg lg:max-w-4xl dark:bg-gray-800">
        {/* Left Side Image */}
        <div
          className="hidden bg-cover lg:block lg:w-1/2"
          style={{
            backgroundImage: `url('https://images.unsplash.com/photo-1606660265514-358ebbadc80d?ixid=MXwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHw%3D&ixlib=rb-1.2.1&auto=format&fit=crop&w=1575&q=80')`,
          }}
        ></div>

        {/* Right Side Form */}
        <div className="w-full px-6 py-8 md:px-8 lg:w-1/2">
          <div className="mx-auto flex justify-center">
            <img
              className="h-7 w-auto sm:h-8"
              src="https://merakiui.com/images/logo.svg"
              alt="Logo"
            />
          </div>

          <p className="mt-3 text-center text-xl text-gray-600 dark:text-gray-200">
            Welcome back!
          </p>

          {/* Divider */}
          <div className="mt-4 flex items-center justify-between">
            <span className="w-1/5 border-b lg:w-1/4 dark:border-gray-600"></span>
            <a
              href="#"
              className="text-center text-xs text-gray-500 uppercase hover:underline dark:text-gray-400"
            >
              login with email
            </a>
            <span className="w-1/5 border-b lg:w-1/4 dark:border-gray-400"></span>
          </div>

          {/* Email Input */}
          <div className="mt-4">
            <label
              className="mb-2 block text-sm font-medium text-gray-600 dark:text-gray-200"
              htmlFor="LoggingEmailAddress"
            >
              Email Address
            </label>
            <input
              id="LoggingEmailAddress"
              className="focus:ring-opacity-40 block w-full rounded-lg border bg-white px-4 py-2 text-gray-700 focus:border-blue-400 focus:ring focus:ring-blue-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-blue-300"
              type="email"
            />
          </div>

          {/* Password Input */}
          <div className="mt-4">
            <div className="flex justify-between">
              <label
                className="mb-2 block text-sm font-medium text-gray-600 dark:text-gray-200"
                htmlFor="loggingPassword"
              >
                Password
              </label>
              <a
                href="#"
                className="text-xs text-gray-500 hover:underline dark:text-gray-300"
              >
                Forget Password?
              </a>
            </div>

            <input
              id="loggingPassword"
              className="focus:ring-opacity-40 block w-full rounded-lg border bg-white px-4 py-2 text-gray-700 focus:border-blue-400 focus:ring focus:ring-blue-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-blue-300"
              type="password"
            />
          </div>

          {/* Sign In Button */}
          <div className="mt-6">
            <button className="focus:ring-opacity-50 w-full transform rounded-lg bg-gray-800 px-6 py-3 text-sm font-medium tracking-wide text-white capitalize transition-colors duration-300 hover:bg-gray-700 focus:ring focus:ring-gray-300 focus:outline-none">
              Sign In
            </button>
          </div>

          {/* Sign Up Link */}
          <div className="mt-4 flex items-center justify-between">
            <span className="w-1/5 border-b md:w-1/4 dark:border-gray-600"></span>
            <a
              href="#"
              className="text-xs text-gray-500 uppercase hover:underline dark:text-gray-400"
            >
              or sign up
            </a>
            <span className="w-1/5 border-b md:w-1/4 dark:border-gray-600"></span>
          </div>
        </div>
      </div>
    </div>
  );
}
