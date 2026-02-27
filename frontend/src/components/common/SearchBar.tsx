const SearchBar = ({
  value,
  onChange,
}: {
  value: string;
  onChange: (value: string) => void;
}) => (
  <div className="mb-8">
    <input
      type="text"
      placeholder="Search for devices (e.g. 'Kitchen Light')..."
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="w-full p-4 rounded-xl border border-gray-200 shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition"
    />
  </div>
);

export default SearchBar;
