(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').factory('schedulerData', schedulerData);

    schedulerData.$inject = ['$http'];

    function schedulerData($http) {
        var service = {
            isRunning: isRunning,
            start: start,
            stop: stop,
            getStats: getStats,
            reScheduleQueue: reScheduleQueue,
            getJobsInfo: getJobsInfo,
            restartJob : restartJob,
        };

        return service;

        function isRunning() {
            return $http.get('/scheduler/running').then(function (d) {
                return d.data;
            });
        }

        function start() {
            $http.post('/scheduler/start');
        }

        function stop() {
            $http.post('/scheduler/stop');
        }

        function restartJob(queueId) {
            $http.post('/scheduler/restartjob/' + queueId);
        }

        function getStats() {
            return $http.get('/scheduler/stats');
        }

        function getJobsInfo() {
            return $http.get('/scheduler/getjobsinfo');
        }

        function reScheduleQueue(queueName) {
            console.log("Reschedule queue " + queueName);
            return $http.post('/scheduler/reschedulefailed/' + queueName);
        }
    }
})(window, window.angular);
