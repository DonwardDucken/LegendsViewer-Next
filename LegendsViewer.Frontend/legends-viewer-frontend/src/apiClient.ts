import createClient from "openapi-fetch";
import type { paths } from "./generated/api-schema";

export const API_BASE_URL = "http://localhost:15421/";

const client = createClient<paths>({ baseUrl: API_BASE_URL });

export default client;