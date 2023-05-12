/**
 * Responds to any HTTP request.
 *
 * @param {!express:Request} req HTTP request context.
 * @param {!express:Response} res HTTP response context.
 */
exports.detectintent = (request, response) => {
  const agent = new WebhookClient({ request, response });

  console.log("Dialogflow Request headers: " + JSON.stringify(request.headers));
  console.log("Dialogflow Request body: " + JSON.stringify(request.body));

  var session_id = request.body.session;
  var session_id_array = session_id.split("/");

  session_id = session_id_array[session_id_array.length - 1];

  return session_id;
};
