function ready(fn) {
  if (
    document.attachEvent
      ? document.readyState === "complete"
      : document.readyState !== "loading"
  ) {
    fn();
  } else {
    document.addEventListener("DOMContentLoaded", fn);
  }
}

// EVENTS BEGIN
const addListeners = () => {
  const dfMessenger = document.querySelector("df-messenger");
  dfMessenger.addEventListener("df-messenger-loaded", function (event) {
    console.log("df-messenger-loaded", event);
    const iddiv = document.querySelector("#session-id");

    iddiv.innerHTML = dfMessenger.getAttribute("session-id");
  });

  dfMessenger.addEventListener("df-messenger-error", function (event) {
    console.log("df-messenger-error", event);
  });

  dfMessenger.addEventListener("df-request-sent", function (event) {
    console.log("df-request-sent", event);
  });

  dfMessenger.addEventListener("df-response-received", function (event) {
    console.log("df-response-received", event);
  });
};

ready(addListeners);
