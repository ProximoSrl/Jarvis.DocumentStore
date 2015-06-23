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
        };

        return service;

        function isRunning() {
            return $http.get('/scheduler/running').then(function (d) {
                return d.data;
            });
        }

        function start() {
            debugger;
            $http.post('/scheduler/start');
        }

        function stop() {
            debugger;
            $http.post('/scheduler/stop');
        }

        function getStats() {
            return $http.get('/scheduler/stats');
        }

        function reScheduleQueue(queueName) {
            console.log("Reschedule queue " + queueName);
            return $http.post('/scheduler/reschedulefailed/' + queueName);
        }
    }
})(window, window.angular);
