const ErrorBanner = ({ message }: { message: string }) => (
  <div className="bg-orange-100 border-l-4 border-orange-500 text-orange-700 p-4 mb-6">
    <p className="font-bold">System Warning</p>
    <p>{message}</p>
  </div>
);

export default ErrorBanner;
