const EmptyState = ({
  hasSearch,
}: {
  hasSearch: boolean;
}) => (
  <div className="text-center mt-10 p-10 bg-white rounded-xl border border-dashed border-gray-300">
    <p className="text-gray-500 text-lg">
      {hasSearch ? "No devices match your search." : "No devices found."}
    </p>
    {!hasSearch && (
      <p className="text-gray-400 text-sm">
        Use the form above to add your first device.
      </p>
    )}
  </div>
);

export default EmptyState;
