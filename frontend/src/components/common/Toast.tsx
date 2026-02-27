const Toast = ({
  message,
  onDismiss,
}: {
  message: string | null;
  onDismiss: () => void;
}) => {
  if (!message) return null;

  return (
    <div className="fixed bottom-4 left-4 right-4 sm:left-auto sm:right-6 sm:bottom-6 sm:w-auto bg-red-600 text-white px-6 py-4 rounded-lg shadow-2xl flex items-center justify-between gap-4 z-50 animate-bounce">
      <span className="font-medium">{message}</span>
      <button
        onClick={onDismiss}
        className="ml-4 hover:text-gray-200 font-bold cursor-pointer"
      >
        ✕
      </button>
    </div>
  );
};

export default Toast;
