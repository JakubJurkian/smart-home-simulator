const LoadingSpinner = ({ label = "Loading..." }: { label?: string }) => (
  <div className="min-h-screen flex items-center justify-center bg-gray-50">
    <div className="text-center">
      <div className="w-16 h-16 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4" />
      <h2 className="text-xl font-semibold text-gray-700">{label}</h2>
    </div>
  </div>
);

export default LoadingSpinner;
