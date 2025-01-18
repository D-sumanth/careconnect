async function loadInformation() {
  try {
    const response = await fetch("/api/get-info");
    const data = await response.json();
    displayInformation(data);
  } catch (error) {
    console.error("Error loading information:", error);
  }
}

function displayInformation(data) {
  // Implementation to display information on the page
}

// Update date in the header
function updateDate() {
  const now = new Date();
  const formattedDate = now.toLocaleDateString("en-GB");
  document.getElementById("currentDate").textContent = formattedDate;
}
