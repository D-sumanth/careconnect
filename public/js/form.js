async function submitForm(event) {
  event.preventDefault();

  // Get selected departments
  const selectedDepartments = Array.from(
    document.querySelectorAll(".option-chip.selected")
  ).map((chip) => chip.textContent);

  const formData = {
    home: document.getElementById("home").value,
    department: document.getElementById("department").value,
    nameDesignation: document.getElementById("nameDesignation").value,
    information: document.getElementById("information").value,
    authorizedBy: document.getElementById("authorizedBy").value,
    state: document.getElementById("state").value,
    sendTo: selectedDepartments,
  };

  console.log("Sending form data:", formData);

  try {
    const response = await fetch("/api/forms", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(formData),
    });

    const result = await response.json();
    console.log("Server response:", result);

    if (response.ok && result.success) {
      //alert('Information saved successfully!');
      window.location.href = "dashboard.html";
    } else {
      throw new Error(result.error || "Failed to save information");
    }
  } catch (error) {
    console.error("Error:", error);
    alert("Error submitting form: " + error.message);
  }
}

// Add event listeners for option chips
document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll(".option-chip").forEach((chip) => {
    chip.addEventListener("click", () => {
      chip.classList.toggle("selected");
    });
  });
});
