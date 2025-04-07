export default function TestFlex() {
  return (
    <div className="flex flex-col h-screen">
      <div className="flex flex-row">
        <div className="flex flex-col flex-1">
          <span>First Panel</span>
        </div>
        <div className="flex flex-col">
          <span>Second Panel</span>
        </div>
      </div>
    </div>
  );
}
